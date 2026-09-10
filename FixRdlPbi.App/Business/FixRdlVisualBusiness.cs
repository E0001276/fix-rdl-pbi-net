using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using FixRdlPbi.App.Clients;
using FixRdlPbi.App.Models;

namespace FixRdlPbi.App.Business;

public class FixRdlVisualBusiness
{
    private readonly FabricApiClient _fabricApiClient;
    private readonly ReportBusiness _reportBusiness;
    private readonly PaginatedReportBusiness _paginatedReportBusiness;
    private readonly SemanticModelBusiness _semanticModelBusiness;
    private readonly ReportDefinitionBusiness _reportDefinitionBusiness;
    private readonly PaginatedReportDefinitionBusiness _paginatedReportDefinitionBusiness;
    private readonly PowerBiApiClient _powerBiApiClient;

    public FixRdlVisualBusiness(
        FabricApiClient fabricApiClient,
        PowerBiApiClient powerBiApiClient)
    {
        _fabricApiClient = fabricApiClient;
        _powerBiApiClient = powerBiApiClient;
        _reportBusiness = new ReportBusiness(fabricApiClient);
        _paginatedReportBusiness = new PaginatedReportBusiness(fabricApiClient);
        _semanticModelBusiness = new SemanticModelBusiness(fabricApiClient);
        _reportDefinitionBusiness = new ReportDefinitionBusiness(fabricApiClient);
        _paginatedReportDefinitionBusiness = new PaginatedReportDefinitionBusiness(fabricApiClient);
    }

    public async Task<FixRdlAnalysis> AnalyzeAsync(
        Workspace sourceWorkspace,
        Workspace targetWorkspace,
        Report sourceReport)
    {
        FixRdlAnalysis analysis = new()
        {
            SourceWorkspace = sourceWorkspace,
            TargetWorkspace = targetWorkspace,
            SourceReport = sourceReport
        };

        List<Report> targetReports = await _reportBusiness.GetReportsAsync(targetWorkspace.Id);
        analysis.TargetReport = FindUniqueByName(
            targetReports,
            sourceReport.DisplayName,
            "Power BI report",
            targetWorkspace.DisplayName
        );

        Task<List<PaginatedReport>> sourcePaginatedTask =
            _paginatedReportBusiness.GetPaginatedReportsAsync(sourceWorkspace.Id);
        Task<List<PaginatedReport>> targetPaginatedTask =
            _paginatedReportBusiness.GetPaginatedReportsAsync(targetWorkspace.Id);
        Task<List<SemanticModel>> targetModelsTask =
            _semanticModelBusiness.GetSemanticModelsAsync(targetWorkspace.Id);
        Task<ReportInspectionResult> sourceInspectionTask =
            _reportDefinitionBusiness.GetInspectionAsync(
                sourceWorkspace.Id,
                sourceWorkspace.DisplayName,
                sourceReport.Id,
                sourceReport.DisplayName
            );
        Task<ReportInspectionResult> targetInspectionTask =
            _reportDefinitionBusiness.GetInspectionAsync(
                targetWorkspace.Id,
                targetWorkspace.DisplayName,
                analysis.TargetReport.Id,
                analysis.TargetReport.DisplayName
            );

        await Task.WhenAll(
            sourcePaginatedTask,
            targetPaginatedTask,
            targetModelsTask,
            sourceInspectionTask,
            targetInspectionTask
        );

        List<PaginatedReport> sourcePaginated = await sourcePaginatedTask;
        List<PaginatedReport> targetPaginated = await targetPaginatedTask;
        List<SemanticModel> targetModels = await targetModelsTask;
        analysis.SourceInspection = await sourceInspectionTask;
        analysis.TargetInspection = await targetInspectionTask;
        analysis.SourceSemanticModel = analysis.SourceInspection.SemanticModel;

        if (analysis.SourceSemanticModel == null ||
            string.IsNullOrWhiteSpace(analysis.SourceSemanticModel.Id))
        {
            throw new InvalidOperationException(
                $"Could not resolve the source semantic model for report '{sourceReport.DisplayName}'."
            );
        }

        string semanticModelName = !string.IsNullOrWhiteSpace(analysis.SourceSemanticModel.DisplayName)
            ? analysis.SourceSemanticModel.DisplayName
            : sourceReport.DisplayName;

        analysis.TargetSemanticModel = FindUniqueByName(
            targetModels,
            semanticModelName,
            "semantic model",
            targetWorkspace.DisplayName
        );

        analysis.SemanticModelGatewayMappings = await ResolveSemanticModelGatewayMappingsAsync(
            analysis.TargetWorkspace,
            analysis.TargetSemanticModel
        );

        analysis.SourcePaginatedById = sourcePaginated
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        analysis.TargetPaginatedByName = targetPaginated
            .GroupBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => x.Count() == 1
                    ? x.Single()
                    : throw new InvalidOperationException(
                        $"More than one paginated report named '{x.Key}' exists in '{targetWorkspace.DisplayName}'."
                    ),
                StringComparer.OrdinalIgnoreCase
            );

        BuildPaginatedMappings(analysis);
        await BuildRowsAsync(analysis);

        return analysis;
    }

    public async Task<string> ApplyAsync(FixRdlAnalysis analysis, bool forceRdlRelink = false)
    {
        if (analysis == null)
        {
            throw new ArgumentNullException(nameof(analysis));
        }

        string backupDirectory = CreateBackupDirectory(analysis);

        ReportDefinitionResponse targetReportDefinition = await GetReportDefinitionAsync(
            analysis.TargetWorkspace.Id,
            analysis.TargetReport.Id
        );

        await SaveBackupAsync(
            backupDirectory,
            $"Report-{SanitizeFileName(analysis.TargetReport.DisplayName)}.json",
            targetReportDefinition
        );

        bool reportChanged = UpdatePowerBiReportDefinition(
            targetReportDefinition,
            analysis,
            forceRdlRelink
        );

        if (reportChanged)
        {
            await UpdateDefinitionAsync(
                $"workspaces/{analysis.TargetWorkspace.Id}/reports/{analysis.TargetReport.Id}/updateDefinition",
                new UpdateDefinitionRequest
                {
                    Definition = targetReportDefinition.Definition
                }
            );

            await VerifyRdlVisualBindingsAsync(analysis);
        }

        bool semanticModelGatewayChanged = false;

        List<SemanticModelGatewayMapping> gatewayMappingsToFix = analysis.SemanticModelGatewayMappings
            .Where(x => x.NeedsFix)
            .ToList();

        if (gatewayMappingsToFix.Count > 0)
        {
            List<string> gatewayIds = analysis.SemanticModelGatewayMappings
                .Where(x => x.Gateway != null)
                .Select(x => x.Gateway.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (gatewayIds.Count != 1)
            {
                throw new InvalidOperationException(
                    "The semantic model could not be resolved to a single on-premises gateway. " +
                    "Automatic remediation was stopped to avoid binding to the wrong gateway."
                );
            }

            BindToGatewayRequest bindRequest = new()
            {
                GatewayObjectId = gatewayIds[0],
                DatasourceObjectIds = null
            };

            await _powerBiApiClient.PostAsync(
                $"groups/{analysis.TargetWorkspace.Id}/datasets/{analysis.TargetSemanticModel.Id}/Default.BindToGateway",
                bindRequest
            );

            semanticModelGatewayChanged = true;
        }

        int paginatedChanged = 0;
        int paginatedDefinitionChanged = 0;
        int paginatedRuntimeChanged = 0;

        foreach (PaginatedReport paginatedReport in analysis.TargetPaginatedReports)
        {
            bool paginatedReportChanged = false;
            string targetServer = BuildPowerBiServer(analysis.TargetWorkspace.DisplayName);
            string targetDatabase = analysis.TargetSemanticModel.DisplayName;

            // First fix the physical RDL definition. UpdateDatasources only changes the
            // runtime connection in Power BI Service; it does not rewrite the RDL that
            // Report Builder downloads. Keeping the RDL itself correct is required so
            // rd:PowerBIWorkspaceName and the virtual server semantic-model ID point to QA.
            ReportDefinitionResponse paginatedDefinition =
                await _paginatedReportDefinitionBusiness.GetDefinitionAsync(
                    analysis.TargetWorkspace.Id,
                    paginatedReport.Id
                );

            await SaveBackupAsync(
                backupDirectory,
                $"Paginated-{SanitizeFileName(paginatedReport.DisplayName)}.json",
                paginatedDefinition
            );

            // Do not force the definition format for paginated reports.
            // Some Fabric tenants currently reject an explicit
            // PaginatedReportDefinition value with InvalidDefinitionFormat,
            // even though the public API documentation lists it as supported.
            // getDefinition without ?format= returns the tenant-native definition,
            // so preserve that response and omit format when it is null.
            bool rdlDefinitionChanged = UpdatePaginatedReportDefinition(
                paginatedDefinition,
                analysis
            );

            if (rdlDefinitionChanged)
            {
                // Use the generic Fabric Item updateDefinition endpoint for paginated reports.
                // In this tenant, the type-specific /paginatedReports/.../updateDefinition
                // endpoint returns InvalidDefinitionFormat even for the documented
                // PaginatedReportDefinition format. The Core Items API accepts the item
                // definition as parts[] and does not require an explicit format value.
                // Preserve the exact RDL part path returned by getDefinition because it is
                // already the canonical path persisted by Fabric.
                ReportDefinition updateDefinition = BuildPaginatedUpdateDefinition(
                    paginatedDefinition,
                    paginatedReport.DisplayName
                );

                await UpdateDefinitionAsync(
                    $"workspaces/{analysis.TargetWorkspace.Id}/items/{paginatedReport.Id}/updateDefinition",
                    new UpdateDefinitionRequest
                    {
                        Definition = updateDefinition
                    }
                );

                await VerifyPaginatedReportDefinitionAsync(analysis, paginatedReport);
                paginatedDefinitionChanged++;
                paginatedReportChanged = true;
            }

            PowerBiDatasourceResponse runtimeDatasources =
                await GetPaginatedRuntimeDatasourcesAsync(
                    analysis.TargetWorkspace.Id,
                    paginatedReport.Id
                );

            bool runtimeAlreadyCorrect = runtimeDatasources.Value.Count > 0 &&
                runtimeDatasources.Value.All(x =>
                    IsTargetDatasource(x, targetServer, targetDatabase));

            if (!runtimeAlreadyCorrect)
            {
                // Read the persisted RDL again because the physical remediation can also
                // rename the datasource (for example wsdevcicd_* -> wsqacicd_*).
                PaginatedReportInspectionResult inspection =
                    await _paginatedReportDefinitionBusiness.GetInspectionAsync(
                        analysis.TargetWorkspace.Id,
                        paginatedReport.Id
                    );

                List<UpdateRdlDatasourceDetail> updateDetails = new();

                foreach (PaginatedDataSource dataSource in inspection.DataSources)
                {
                    if (string.IsNullOrWhiteSpace(dataSource.Name))
                    {
                        throw new InvalidOperationException(
                            $"Paginated report '{paginatedReport.DisplayName}' contains a data source without a name."
                        );
                    }

                    updateDetails.Add(new UpdateRdlDatasourceDetail
                    {
                        DatasourceName = dataSource.Name,
                        ConnectionDetails = new RdlDatasourceConnectionDetails
                        {
                            Server = targetServer,
                            Database = targetDatabase
                        }
                    });
                }

                if (updateDetails.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Paginated report '{paginatedReport.DisplayName}' has no RDL data sources to update."
                    );
                }

                // UpdateDatasources for RDL reports requires the caller to own the
                // paginated report data sources. TakeOver is idempotent for this use case.
                await _powerBiApiClient.PostAsync(
                    $"groups/{analysis.TargetWorkspace.Id}/reports/{paginatedReport.Id}/Default.TakeOver"
                );

                UpdateRdlDatasourcesRequest request = new()
                {
                    UpdateDetails = updateDetails
                };

                await _powerBiApiClient.PostAsync(
                    $"groups/{analysis.TargetWorkspace.Id}/reports/{paginatedReport.Id}/Default.UpdateDatasources",
                    request
                );

                paginatedRuntimeChanged++;
                paginatedReportChanged = true;
            }

            if (paginatedReportChanged)
            {
                paginatedChanged++;
            }
        }

        return $"Completed. Power BI report updated: {(reportChanged ? "Yes" : "No")}. " +
               $"Semantic model gateway updated: {(semanticModelGatewayChanged ? "Yes" : "No")}. " +
               $"Paginated RDL definitions updated: {paginatedDefinitionChanged}. " +
               $"Paginated runtime datasources updated: {paginatedRuntimeChanged}. " +
               $"Paginated reports changed: {paginatedChanged}. Backup: {backupDirectory}";
    }

    private void BuildPaginatedMappings(FixRdlAnalysis analysis)
    {
        HashSet<string> targetIds = new(StringComparer.OrdinalIgnoreCase);

        foreach (RdlVisualReference sourceReference in analysis.SourceInspection.RdlVisualReferences)
        {
            if (!analysis.SourcePaginatedById.TryGetValue(
                sourceReference.ReportId,
                out PaginatedReport sourcePaginated))
            {
                throw new InvalidOperationException(
                    $"Source RDL visual on page '{sourceReference.PageName}' references paginated report " +
                    $"'{sourceReference.ReportId}', but that item was not found in source workspace '{analysis.SourceWorkspace.DisplayName}'."
                );
            }

            if (!analysis.TargetPaginatedByName.TryGetValue(
                sourcePaginated.DisplayName,
                out PaginatedReport targetPaginated))
            {
                throw new InvalidOperationException(
                    $"Target paginated report '{sourcePaginated.DisplayName}' was not found in workspace '{analysis.TargetWorkspace.DisplayName}'."
                );
            }

            analysis.SourcePaginatedIdToTargetId[sourcePaginated.Id] = targetPaginated.Id;
            targetIds.Add(targetPaginated.Id);
        }

        analysis.TargetPaginatedReports = analysis.TargetPaginatedByName.Values
            .Where(x => targetIds.Contains(x.Id))
            .OrderBy(x => x.DisplayName)
            .ToList();

        foreach (RdlVisualReference targetReference in analysis.TargetInspection.RdlVisualReferences)
        {
            string desiredTargetId = ResolveDesiredPaginatedId(analysis, targetReference);

            if (!string.IsNullOrWhiteSpace(desiredTargetId))
            {
                analysis.TargetCurrentPaginatedIdToTargetId[targetReference.ReportId] = desiredTargetId;
            }
        }
    }

    private async Task BuildRowsAsync(FixRdlAnalysis analysis)
    {
        analysis.Rows.Clear();

        string currentSemanticModelId = analysis.TargetInspection.SemanticModel == null
            ? ExtractSemanticModelId(analysis.TargetInspection.SemanticModelReference)
            : analysis.TargetInspection.SemanticModel.Id;

        analysis.Rows.Add(new FixRdlPlanRow
        {
            ArtifactType = "Power BI Report",
            ArtifactName = analysis.TargetReport.DisplayName,
            Property = "Semantic model",
            CurrentValue = currentSemanticModelId,
            TargetValue = analysis.TargetSemanticModel.Id,
            Status = EqualsIgnoreCase(currentSemanticModelId, analysis.TargetSemanticModel.Id)
                ? "Correct"
                : "Needs fix"
        });

        foreach (SemanticModelGatewayMapping mapping in analysis.SemanticModelGatewayMappings)
        {
            PowerBiDatasource modelDatasource = mapping.ModelDatasource;
            string currentValue = FormatSemanticModelBindingCurrent(modelDatasource);
            string targetValue = mapping.Gateway != null
                ? FormatSemanticModelBindingTarget(mapping.Gateway, mapping.GatewayDatasource)
                : mapping.Error;

            analysis.Rows.Add(new FixRdlPlanRow
            {
                ArtifactType = "Semantic Model",
                ArtifactName = analysis.TargetSemanticModel.DisplayName,
                Property = $"Gateway binding ({modelDatasource.DatasourceType})",
                CurrentValue = currentValue,
                TargetValue = targetValue,
                Status = mapping.Status
            });
        }

        foreach (RdlVisualReference targetReference in analysis.TargetInspection.RdlVisualReferences)
        {
            string desiredTargetId = ResolveDesiredPaginatedId(analysis, targetReference);

            if (string.IsNullOrWhiteSpace(desiredTargetId))
            {
                analysis.Rows.Add(new FixRdlPlanRow
                {
                    ArtifactType = "RDL Visual",
                    ArtifactName = targetReference.PageName,
                    Property = "Paginated report",
                    CurrentValue = targetReference.ReportId,
                    TargetValue = "Unable to resolve",
                    Status = "Error"
                });
                continue;
            }

            PaginatedReport desiredReport = analysis.TargetPaginatedReports
                .First(x => EqualsIgnoreCase(x.Id, desiredTargetId));

            analysis.Rows.Add(new FixRdlPlanRow
            {
                ArtifactType = "RDL Visual",
                ArtifactName = targetReference.PageName,
                Property = desiredReport.DisplayName,
                CurrentValue = targetReference.ReportId,
                TargetValue = desiredTargetId,
                Status = targetReference.IsCanonicalItemLocation
                    && string.Equals(targetReference.ReferenceKind, "ItemLocation", StringComparison.OrdinalIgnoreCase)
                    && EqualsIgnoreCase(targetReference.ReportId, desiredTargetId)
                    && EqualsIgnoreCase(targetReference.WorkspaceId, analysis.TargetWorkspace.Id)
                    ? "Correct"
                    : "Needs fix"
            });
        }

        foreach (PaginatedReport paginatedReport in analysis.TargetPaginatedReports)
        {
            string targetServer = BuildPowerBiServer(analysis.TargetWorkspace.DisplayName);
            string targetDatabase = analysis.TargetSemanticModel.DisplayName;

            PaginatedReportInspectionResult rdlInspection =
                await _paginatedReportDefinitionBusiness.GetInspectionAsync(
                    analysis.TargetWorkspace.Id,
                    paginatedReport.Id
                );

            if (rdlInspection.DataSources.Count == 0)
            {
                analysis.Rows.Add(new FixRdlPlanRow
                {
                    ArtifactType = "Paginated Report",
                    ArtifactName = paginatedReport.DisplayName,
                    Property = "RDL definition",
                    CurrentValue = "No embedded RDL data source found",
                    TargetValue = FormatRdlDefinitionTarget(analysis, string.Empty),
                    Status = "Error"
                });
            }
            else
            {
                foreach (PaginatedDataSource dataSource in rdlInspection.DataSources)
                {
                    analysis.Rows.Add(new FixRdlPlanRow
                    {
                        ArtifactType = "Paginated Report",
                        ArtifactName = paginatedReport.DisplayName,
                        Property = "RDL definition",
                        CurrentValue = FormatRdlDefinitionCurrent(dataSource),
                        TargetValue = FormatRdlDefinitionTarget(analysis, dataSource.Name),
                        Status = IsTargetRdlDefinition(dataSource, analysis)
                            ? "Correct"
                            : "Needs fix"
                    });
                }
            }

            PowerBiDatasourceResponse runtimeDatasources =
                await GetPaginatedRuntimeDatasourcesAsync(
                    analysis.TargetWorkspace.Id,
                    paginatedReport.Id
                );

            if (runtimeDatasources.Value.Count == 0)
            {
                analysis.Rows.Add(new FixRdlPlanRow
                {
                    ArtifactType = "Paginated Report",
                    ArtifactName = paginatedReport.DisplayName,
                    Property = "Runtime data source",
                    CurrentValue = "No data source returned by Power BI API",
                    TargetValue = FormatDatasource(targetServer, targetDatabase),
                    Status = "Error"
                });
                continue;
            }

            foreach (PowerBiDatasource dataSource in runtimeDatasources.Value)
            {
                string currentServer = dataSource.ConnectionDetails?.Server ?? string.Empty;
                string currentDatabase = dataSource.ConnectionDetails?.Database ?? string.Empty;

                analysis.Rows.Add(new FixRdlPlanRow
                {
                    ArtifactType = "Paginated Report",
                    ArtifactName = paginatedReport.DisplayName,
                    Property = "Runtime data source",
                    CurrentValue = FormatDatasource(currentServer, currentDatabase),
                    TargetValue = FormatDatasource(targetServer, targetDatabase),
                    Status = IsTargetDatasource(dataSource, targetServer, targetDatabase)
                        ? "Correct"
                        : "Needs fix"
                });
            }
        }
    }

    private static string ResolveDesiredPaginatedId(
        FixRdlAnalysis analysis,
        RdlVisualReference targetReference)
    {
        if (analysis.SourcePaginatedIdToTargetId.TryGetValue(
            targetReference.ReportId,
            out string mappedId))
        {
            return mappedId;
        }

        PaginatedReport alreadyTarget = analysis.TargetPaginatedReports.FirstOrDefault(
            x => EqualsIgnoreCase(x.Id, targetReference.ReportId)
        );

        if (alreadyTarget != null)
        {
            return alreadyTarget.Id;
        }

        RdlVisualReference sourceSamePage = analysis.SourceInspection.RdlVisualReferences
            .FirstOrDefault(
                x => string.Equals(
                    x.PageName,
                    targetReference.PageName,
                    StringComparison.OrdinalIgnoreCase
                )
            );

        if (sourceSamePage != null &&
            analysis.SourcePaginatedIdToTargetId.TryGetValue(
                sourceSamePage.ReportId,
                out mappedId))
        {
            return mappedId;
        }

        return string.Empty;
    }

    private static bool UpdatePowerBiReportDefinition(
        ReportDefinitionResponse definition,
        FixRdlAnalysis analysis,
        bool forceRdlRelink)
    {
        bool changed = false;

        foreach (ReportDefinitionPart part in definition.Definition.Parts)
        {
            if (string.Equals(part.Path, "definition.pbir", StringComparison.OrdinalIgnoreCase))
            {
                string currentSemanticModelId = analysis.TargetInspection.SemanticModel == null
                    ? ExtractSemanticModelId(analysis.TargetInspection.SemanticModelReference)
                    : analysis.TargetInspection.SemanticModel.Id;

                if (!EqualsIgnoreCase(currentSemanticModelId, analysis.TargetSemanticModel.Id))
                {
                    JsonNode root = JsonNode.Parse(DecodeBase64Utf8(part.Payload));

                    if (root is JsonObject rootObject)
                    {
                        JsonObject byConnection = new()
                        {
                            ["connectionString"] = $"semanticmodelid={analysis.TargetSemanticModel.Id}"
                        };

                        JsonObject datasetReference = new()
                        {
                            ["byConnection"] = byConnection
                        };

                        rootObject["datasetReference"] = datasetReference;
                        part.Payload = EncodeBase64Utf8(rootObject.ToJsonString(JsonIndentedOptions()));
                        changed = true;
                    }
                }

                continue;
            }

            if (!part.Path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            JsonNode json;

            try
            {
                json = JsonNode.Parse(DecodeBase64Utf8(part.Payload));
            }
            catch (JsonException)
            {
                continue;
            }

            if (json == null)
            {
                continue;
            }

            bool partChanged = false;

            if (part.Path.EndsWith("/visual.json", StringComparison.OrdinalIgnoreCase))
            {
                RdlVisualReference targetReference = analysis.TargetInspection.RdlVisualReferences
                    .FirstOrDefault(x => string.Equals(
                        x.VisualPath,
                        part.Path,
                        StringComparison.OrdinalIgnoreCase
                    ));

                if (targetReference != null)
                {
                    string desiredItemId = ResolveDesiredPaginatedId(analysis, targetReference);

                    if (!string.IsNullOrWhiteSpace(desiredItemId))
                    {
                        partChanged = UpdateSingleRdlVisual(
                            json,
                            analysis.TargetWorkspace.Id,
                            desiredItemId,
                            forceRdlRelink
                        );
                    }
                }
            }

            if (partChanged)
            {
                part.Payload = EncodeBase64Utf8(json.ToJsonString(JsonIndentedOptions()));
                changed = true;
            }
        }

        return changed;
    }

    private static bool UpdateSingleRdlVisual(
        JsonNode root,
        string targetWorkspaceId,
        string targetItemId,
        bool forceRdlRelink)
    {
        if (root is not JsonObject rootObject ||
            rootObject["visual"] is not JsonObject visualObject ||
            !TryGetStringValue(visualObject["visualType"], out string visualType) ||
            !string.Equals(visualType, "rdlVisual", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return ReplaceRdlReference(
            visualObject,
            targetWorkspaceId,
            targetItemId,
            forceRdlRelink
        );
    }

    private static bool ReplaceRdlReference(
        JsonObject visualObject,
        string targetWorkspaceId,
        string targetItemId,
        bool forceRdlRelink)
    {
        JsonObject objectsObject = visualObject["objects"] as JsonObject;
        if (objectsObject == null)
        {
            objectsObject = new JsonObject();
            visualObject["objects"] = objectsObject;
        }

        JsonArray reportInfoArray = objectsObject["reportInfo"] as JsonArray;
        if (reportInfoArray == null)
        {
            reportInfoArray = new JsonArray();
            objectsObject["reportInfo"] = reportInfoArray;
        }

        JsonObject reportInfoObject;
        if (reportInfoArray.Count == 0 || reportInfoArray[0] is not JsonObject existingReportInfo)
        {
            reportInfoObject = new JsonObject();
            if (reportInfoArray.Count == 0)
            {
                reportInfoArray.Add(reportInfoObject);
            }
            else
            {
                reportInfoArray[0] = reportInfoObject;
            }
        }
        else
        {
            reportInfoObject = existingReportInfo;
        }

        JsonObject propertiesObject = reportInfoObject["properties"] as JsonObject;
        if (propertiesObject == null)
        {
            propertiesObject = new JsonObject();
            reportInfoObject["properties"] = propertiesObject;
        }

        JsonObject existingReference = propertiesObject["reference"] as JsonObject;
        string existingKind = string.Empty;
        string existingWorkspaceId = string.Empty;
        string existingItemId = string.Empty;

        if (existingReference != null)
        {
            TryGetStringValue(existingReference["kind"], out existingKind);
            if (existingReference["byReference"] is JsonObject existingByReference)
            {
                existingWorkspaceId = GetLiteralValue(existingByReference, "workspaceId");
                existingItemId = GetLiteralValue(existingByReference, "itemId");
            }
        }

        bool alreadyCanonical =
            string.Equals(existingKind, "ItemLocation", StringComparison.OrdinalIgnoreCase) &&
            EqualsIgnoreCase(existingWorkspaceId, targetWorkspaceId) &&
            EqualsIgnoreCase(existingItemId, targetItemId);

        if (alreadyCanonical && !forceRdlRelink)
        {
            return false;
        }

        JsonObject byReference = new()
        {
            ["itemId"] = CreateLiteralProperty(targetItemId),
            ["workspaceId"] = CreateLiteralProperty(targetWorkspaceId)
        };

        propertiesObject["reference"] = new JsonObject
        {
            ["kind"] = "ItemLocation",
            ["byReference"] = byReference
        };

        return true;
    }

    private static JsonObject CreateLiteralProperty(string value)
    {
        return new JsonObject
        {
            ["expr"] = new JsonObject
            {
                ["Literal"] = new JsonObject
                {
                    ["Value"] = $"'{value}'"
                }
            }
        };
    }

    private static JsonObject GetRdlByReferenceObject(JsonObject visualObject)
    {
        if (visualObject["objects"] is not JsonObject objectsObject ||
            objectsObject["reportInfo"] is not JsonArray reportInfoArray ||
            reportInfoArray.Count == 0 ||
            reportInfoArray[0] is not JsonObject reportInfoObject ||
            reportInfoObject["properties"] is not JsonObject propertiesObject ||
            propertiesObject["reference"] is not JsonObject referenceObject ||
            referenceObject["byReference"] is not JsonObject byReferenceObject)
        {
            return null;
        }

        return byReferenceObject;
    }

    private static string GetLiteralValue(JsonObject referenceObject, string propertyName)
    {
        if (referenceObject[propertyName] is not JsonObject propertyObject ||
            propertyObject["expr"] is not JsonObject exprObject ||
            exprObject["Literal"] is not JsonObject literalObject ||
            !TryGetStringValue(literalObject["Value"], out string value))
        {
            return string.Empty;
        }

        return TrimLiteral(value);
    }

    private static bool SetLiteralValue(JsonObject referenceObject, string propertyName, string value)
    {
        if (referenceObject[propertyName] is not JsonObject propertyObject ||
            propertyObject["expr"] is not JsonObject exprObject ||
            exprObject["Literal"] is not JsonObject literalObject)
        {
            return false;
        }

        literalObject["Value"] = $"'{value}'";
        return true;
    }

    private static bool TryGetStringValue(JsonNode node, out string value)
    {
        value = string.Empty;

        if (node is not JsonValue jsonValue ||
            !jsonValue.TryGetValue(out string stringValue) ||
            string.IsNullOrWhiteSpace(stringValue))
        {
            return false;
        }

        value = stringValue;
        return true;
    }

    private async Task VerifyRdlVisualBindingsAsync(FixRdlAnalysis analysis)
    {
        ReportInspectionResult persisted = await _reportDefinitionBusiness.GetInspectionAsync(
            analysis.TargetWorkspace.Id,
            analysis.TargetWorkspace.DisplayName,
            analysis.TargetReport.Id,
            analysis.TargetReport.DisplayName
        );

        foreach (RdlVisualReference sourceReference in analysis.SourceInspection.RdlVisualReferences)
        {
            if (!analysis.SourcePaginatedById.TryGetValue(sourceReference.ReportId, out PaginatedReport sourcePaginated) ||
                !analysis.TargetPaginatedByName.TryGetValue(sourcePaginated.DisplayName, out PaginatedReport expectedTarget))
            {
                continue;
            }

            RdlVisualReference persistedReference = persisted.RdlVisualReferences.FirstOrDefault(
                x => string.Equals(x.PageName, sourceReference.PageName, StringComparison.OrdinalIgnoreCase)
            );

            if (persistedReference == null ||
                !persistedReference.IsCanonicalItemLocation ||
                !EqualsIgnoreCase(persistedReference.WorkspaceId, analysis.TargetWorkspace.Id) ||
                !EqualsIgnoreCase(persistedReference.ReportId, expectedTarget.Id))
            {
                throw new InvalidOperationException(
                    $"Fabric accepted the report update, but RDL visual '{sourceReference.PageName}' was not persisted " +
                    $"with the expected target binding. Expected workspace '{analysis.TargetWorkspace.Id}' and item " +
                    $"'{expectedTarget.Id}', but found workspace '{persistedReference?.WorkspaceId ?? "(missing)"}' and item " +
                    $"'{persistedReference?.ReportId ?? "(missing)"}'."
                );
            }
        }
    }

    private async Task VerifyPaginatedReportDefinitionAsync(
        FixRdlAnalysis analysis,
        PaginatedReport paginatedReport)
    {
        PaginatedReportInspectionResult persisted =
            await _paginatedReportDefinitionBusiness.GetInspectionAsync(
                analysis.TargetWorkspace.Id,
                paginatedReport.Id
            );

        if (persisted.DataSources.Count == 0)
        {
            throw new InvalidOperationException(
                $"Fabric accepted the paginated report update, but '{paginatedReport.DisplayName}' " +
                "does not contain an embedded RDL data source after the update."
            );
        }

        List<PaginatedDataSource> invalid = persisted.DataSources
            .Where(x => !IsTargetRdlDefinition(x, analysis))
            .ToList();

        if (invalid.Count > 0)
        {
            string details = string.Join(
                " | ",
                invalid.Select(FormatRdlDefinitionCurrent)
            );

            throw new InvalidOperationException(
                $"Fabric accepted the paginated report definition update, but '{paginatedReport.DisplayName}' " +
                $"was not persisted with the expected QA semantic model reference. Found: {details}"
            );
        }
    }

    private static bool IsTargetRdlDefinition(
        PaginatedDataSource dataSource,
        FixRdlAnalysis analysis)
    {
        if (dataSource == null)
        {
            return false;
        }

        string targetDatasourceName = BuildTargetRdlDatasourceName(
            dataSource.Name,
            analysis.SourceWorkspace,
            analysis.TargetWorkspace
        );

        string semanticModelId = ExtractSemanticModelId(dataSource.ConnectionString);

        return string.Equals(
                dataSource.Name,
                targetDatasourceName,
                StringComparison.OrdinalIgnoreCase) &&
            string.Equals(
                dataSource.PowerBIWorkspaceName,
                analysis.TargetWorkspace.DisplayName,
                StringComparison.OrdinalIgnoreCase) &&
            string.Equals(
                dataSource.PowerBIDatasetName,
                analysis.TargetSemanticModel.DisplayName,
                StringComparison.OrdinalIgnoreCase) &&
            string.Equals(
                semanticModelId,
                analysis.TargetSemanticModel.Id,
                StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatRdlDefinitionCurrent(PaginatedDataSource dataSource)
    {
        string semanticModelId = ExtractSemanticModelId(dataSource.ConnectionString);
        return $"Name={dataSource.Name}; Workspace={dataSource.PowerBIWorkspaceName}; " +
               $"Dataset={dataSource.PowerBIDatasetName}; SemanticModelId={semanticModelId}";
    }

    private static string FormatRdlDefinitionTarget(
        FixRdlAnalysis analysis,
        string currentDatasourceName)
    {
        string targetDatasourceName = BuildTargetRdlDatasourceName(
            currentDatasourceName,
            analysis.SourceWorkspace,
            analysis.TargetWorkspace
        );

        return $"Name={targetDatasourceName}; Workspace={analysis.TargetWorkspace.DisplayName}; " +
               $"Dataset={analysis.TargetSemanticModel.DisplayName}; SemanticModelId={analysis.TargetSemanticModel.Id}";
    }


    private static ReportDefinition BuildPaginatedUpdateDefinition(
        ReportDefinitionResponse sourceDefinition,
        string paginatedReportDisplayName)
    {
        if (sourceDefinition == null || sourceDefinition.Definition == null)
        {
            throw new InvalidOperationException(
                "The paginated report definition is empty."
            );
        }

        List<ReportDefinitionPart> rdlParts = sourceDefinition.Definition.Parts
            .Where(x => x != null &&
                !string.IsNullOrWhiteSpace(x.Path) &&
                x.Path.EndsWith(".rdl", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (rdlParts.Count != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one RDL definition part, but found {rdlParts.Count}."
            );
        }

        ReportDefinitionPart sourceRdl = rdlParts[0];
        if (string.IsNullOrWhiteSpace(sourceRdl.Payload))
        {
            throw new InvalidOperationException(
                $"The RDL definition part for '{paginatedReportDisplayName}' has an empty payload."
            );
        }

        if (string.IsNullOrWhiteSpace(sourceRdl.Path))
        {
            throw new InvalidOperationException(
                $"The RDL definition part for '{paginatedReportDisplayName}' has an empty path."
            );
        }

        return new ReportDefinition
        {
            // Intentionally omit format. The generic Core Items updateDefinition API
            // accepts parts[] directly, and omitting format avoids the tenant-specific
            // InvalidDefinitionFormat response seen with PaginatedReportDefinition.
            Format = null,
            Parts = new List<ReportDefinitionPart>
            {
                new ReportDefinitionPart
                {
                    // Keep the exact path returned by Fabric getDefinition.
                    Path = sourceRdl.Path,
                    Payload = sourceRdl.Payload,
                    PayloadType = string.IsNullOrWhiteSpace(sourceRdl.PayloadType)
                        ? "InlineBase64"
                        : sourceRdl.PayloadType
                }
            }
        };
    }

    private static bool UpdatePaginatedReportDefinition(
        ReportDefinitionResponse definition,
        FixRdlAnalysis analysis)
    {
        bool changed = false;

        foreach (ReportDefinitionPart part in definition.Definition.Parts)
        {
            if (!part.Path.EndsWith(".rdl", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            XDocument document = LoadXmlDocumentFromBase64(part);
            bool partChanged = false;
            Dictionary<string, string> datasourceNameMappings =
                new(StringComparer.OrdinalIgnoreCase);

            foreach (XElement dataSourceElement in document
                .Descendants()
                .Where(x => string.Equals(
                    x.Name.LocalName,
                    "DataSource",
                    StringComparison.OrdinalIgnoreCase)))
            {
                XAttribute nameAttribute = dataSourceElement.Attributes()
                    .FirstOrDefault(x => string.Equals(
                        x.Name.LocalName,
                        "Name",
                        StringComparison.OrdinalIgnoreCase));

                if (nameAttribute != null && !string.IsNullOrWhiteSpace(nameAttribute.Value))
                {
                    string targetName = BuildTargetRdlDatasourceName(
                        nameAttribute.Value,
                        analysis.SourceWorkspace,
                        analysis.TargetWorkspace
                    );

                    datasourceNameMappings[nameAttribute.Value] = targetName;

                    if (!string.Equals(
                        nameAttribute.Value,
                        targetName,
                        StringComparison.Ordinal))
                    {
                        nameAttribute.Value = targetName;
                        partChanged = true;
                    }
                }
            }

            foreach (XElement element in document.Descendants())
            {
                if (string.Equals(
                    element.Name.LocalName,
                    "ConnectString",
                    StringComparison.OrdinalIgnoreCase))
                {
                    string updated = UpdateConnectionString(
                        element.Value,
                        analysis.SourceWorkspace,
                        analysis.TargetWorkspace,
                        analysis.SourceSemanticModel,
                        analysis.TargetSemanticModel
                    );

                    if (!string.Equals(element.Value, updated, StringComparison.Ordinal))
                    {
                        element.Value = updated;
                        partChanged = true;
                    }
                }
                else if (string.Equals(
                    element.Name.LocalName,
                    "PowerBIWorkspaceName",
                    StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(
                        element.Value,
                        analysis.TargetWorkspace.DisplayName,
                        StringComparison.Ordinal))
                    {
                        element.Value = analysis.TargetWorkspace.DisplayName;
                        partChanged = true;
                    }
                }
                else if (string.Equals(
                    element.Name.LocalName,
                    "PowerBIDatasetName",
                    StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(
                        element.Value,
                        analysis.TargetSemanticModel.DisplayName,
                        StringComparison.Ordinal))
                    {
                        element.Value = analysis.TargetSemanticModel.DisplayName;
                        partChanged = true;
                    }
                }
                else if (string.Equals(
                    element.Name.LocalName,
                    "DataSourceName",
                    StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(element.Value))
                {
                    string targetName = datasourceNameMappings.TryGetValue(
                        element.Value,
                        out string mappedName)
                        ? mappedName
                        : BuildTargetRdlDatasourceName(
                            element.Value,
                            analysis.SourceWorkspace,
                            analysis.TargetWorkspace
                        );

                    if (!string.Equals(
                        element.Value,
                        targetName,
                        StringComparison.Ordinal))
                    {
                        element.Value = targetName;
                        partChanged = true;
                    }
                }
            }

            if (partChanged)
            {
                part.Payload = EncodeXmlDocumentBase64(document);
                part.PayloadType = "InlineBase64";
                changed = true;
            }
        }

        return changed;
    }

    private static string BuildTargetRdlDatasourceName(
        string currentName,
        Workspace sourceWorkspace,
        Workspace targetWorkspace)
    {
        if (string.IsNullOrWhiteSpace(currentName))
        {
            return currentName;
        }

        string sourcePrefix = NormalizeWorkspaceForRdlDatasource(sourceWorkspace.DisplayName);
        string targetPrefix = NormalizeWorkspaceForRdlDatasource(targetWorkspace.DisplayName);

        if (!string.IsNullOrWhiteSpace(sourcePrefix) &&
            currentName.StartsWith(sourcePrefix + "_", StringComparison.OrdinalIgnoreCase))
        {
            return targetPrefix + currentName[sourcePrefix.Length..];
        }

        return currentName;
    }

    private static string NormalizeWorkspaceForRdlDatasource(string workspaceName)
    {
        if (string.IsNullOrWhiteSpace(workspaceName))
        {
            return string.Empty;
        }

        return Regex.Replace(workspaceName, @"[\s-]+", string.Empty);
    }

    private static string UpdateConnectionString(
        string connectionString,
        Workspace sourceWorkspace,
        Workspace targetWorkspace,
        SemanticModel sourceModel,
        SemanticModel targetModel)
    {
        string result = connectionString;

        if (!string.IsNullOrWhiteSpace(sourceModel.Id))
        {
            result = ReplaceIgnoreCase(result, sourceModel.Id, targetModel.Id);
        }

        if (!string.IsNullOrWhiteSpace(sourceWorkspace.Id))
        {
            result = ReplaceIgnoreCase(result, sourceWorkspace.Id, targetWorkspace.Id);
        }

        if (!string.IsNullOrWhiteSpace(sourceWorkspace.DisplayName))
        {
            result = ReplaceIgnoreCase(
                result,
                sourceWorkspace.DisplayName,
                targetWorkspace.DisplayName
            );
        }

        result = Regex.Replace(
            result,
            @"(?i)(Initial\s+Catalog\s*=\s*sobe_wowvirtualserver-)[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}",
            match => match.Groups[1].Value + targetModel.Id
        );

        result = Regex.Replace(
            result,
            @"(?i)(semanticmodelid\s*=\s*)[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}",
            match => match.Groups[1].Value + targetModel.Id
        );

        return result;
    }

    private async Task<List<SemanticModelGatewayMapping>> ResolveSemanticModelGatewayMappingsAsync(
        Workspace targetWorkspace,
        SemanticModel targetSemanticModel)
    {
        PowerBiDatasourceResponse datasourceResponse = await _powerBiApiClient.GetAsync<PowerBiDatasourceResponse>(
            $"groups/{targetWorkspace.Id}/datasets/{targetSemanticModel.Id}/datasources"
        );

        List<PowerBiDatasource> modelDatasources = datasourceResponse.Value
            .Where(x => string.Equals(x.DatasourceType, "Oracle", StringComparison.OrdinalIgnoreCase))
            .ToList();

        List<SemanticModelGatewayMapping> mappings = new();

        if (modelDatasources.Count == 0)
        {
            return mappings;
        }

        PowerBiGatewayResponse gatewaysResponse;

        try
        {
            gatewaysResponse = await _powerBiApiClient.GetAsync<PowerBiGatewayResponse>(
                $"groups/{targetWorkspace.Id}/datasets/{targetSemanticModel.Id}/Default.DiscoverGateways"
            );
        }
        catch (HttpRequestException ex)
        {
            foreach (PowerBiDatasource modelDatasource in modelDatasources)
            {
                mappings.Add(new SemanticModelGatewayMapping
                {
                    ModelDatasource = modelDatasource,
                    Status = "Error",
                    Error = "Unable to discover gateways that can bind this semantic model. " + ex.Message
                });
            }

            return mappings;
        }

        List<PowerBiGateway> gateways = gatewaysResponse.Value
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();

        foreach (PowerBiDatasource modelDatasource in modelDatasources)
        {
            if (!string.IsNullOrWhiteSpace(modelDatasource.GatewayId))
            {
                PowerBiGateway currentGateway = gateways.FirstOrDefault(x =>
                    EqualsIgnoreCase(x.Id, modelDatasource.GatewayId));

                if (currentGateway != null && !string.IsNullOrWhiteSpace(modelDatasource.DatasourceId))
                {
                    mappings.Add(new SemanticModelGatewayMapping
                    {
                        ModelDatasource = modelDatasource,
                        Gateway = currentGateway,
                        Status = "Correct"
                    });
                    continue;
                }
            }

            if (gateways.Count == 1)
            {
                mappings.Add(new SemanticModelGatewayMapping
                {
                    ModelDatasource = modelDatasource,
                    Gateway = gateways[0],
                    Status = "Needs fix"
                });
                continue;
            }

            if (gateways.Count == 0)
            {
                mappings.Add(new SemanticModelGatewayMapping
                {
                    ModelDatasource = modelDatasource,
                    Status = "Error",
                    Error = "No on-premises gateway was discovered for " +
                        FormatDatasourceConnection(modelDatasource) +
                        ". Verify that a matching Oracle datasource exists and that the current user is allowed to use it."
                });
                continue;
            }

            string gatewayNames = string.Join(", ", gateways.Select(x => $"{x.Name} ({x.Id})"));

            mappings.Add(new SemanticModelGatewayMapping
            {
                ModelDatasource = modelDatasource,
                Status = "Error",
                Error = "More than one gateway can bind this semantic model. Automatic selection is unsafe. " +
                    $"Candidates: {gatewayNames}"
            });
        }

        return mappings;
    }

    private static string FormatSemanticModelBindingCurrent(PowerBiDatasource datasource)
    {
        string gateway = string.IsNullOrWhiteSpace(datasource.GatewayId)
            ? "Unbound"
            : datasource.GatewayId;
        string datasourceId = string.IsNullOrWhiteSpace(datasource.DatasourceId)
            ? "Unbound"
            : datasource.DatasourceId;

        return $"Gateway={gateway}; Datasource={datasourceId}; {FormatDatasourceConnection(datasource)}";
    }

    private static string FormatSemanticModelBindingTarget(
        PowerBiGateway gateway,
        PowerBiGatewayDatasource datasource)
    {
        if (datasource == null)
        {
            return $"Gateway={gateway.Name} ({gateway.Id}); Datasource=first matching datasource selected by Power BI";
        }

        PowerBiDatasourceConnectionDetails connection = datasource.ParseConnectionDetails();
        string connectionText = FormatDatasource(connection.Server, connection.Database);
        return $"Gateway={gateway.Name} ({gateway.Id}); Datasource={datasource.DatasourceName} ({datasource.Id}); {connectionText}";
    }

    private static string FormatDatasourceConnection(PowerBiDatasource datasource)
    {
        PowerBiDatasourceConnectionDetails connection = datasource.ConnectionDetails ??
            new PowerBiDatasourceConnectionDetails();
        return $"Type={datasource.DatasourceType}; {FormatDatasource(connection.Server, connection.Database)}";
    }

    private async Task<PowerBiDatasourceResponse> GetPaginatedRuntimeDatasourcesAsync(
        string workspaceId,
        string reportId)
    {
        return await _powerBiApiClient.GetAsync<PowerBiDatasourceResponse>(
            $"groups/{workspaceId}/reports/{reportId}/datasources"
        );
    }

    private static string BuildPowerBiServer(string workspaceName)
    {
        return $"powerbi://api.powerbi.com/v1.0/myorg/{workspaceName}";
    }

    private static bool IsTargetDatasource(
        PowerBiDatasource dataSource,
        string targetServer,
        string targetDatabase)
    {
        if (dataSource == null || dataSource.ConnectionDetails == null)
        {
            return false;
        }

        return EqualsNormalizedConnectionValue(
                dataSource.ConnectionDetails.Server,
                targetServer) &&
            EqualsNormalizedConnectionValue(
                dataSource.ConnectionDetails.Database,
                targetDatabase);
    }

    private static bool EqualsNormalizedConnectionValue(string left, string right)
    {
        return string.Equals(
            (left ?? string.Empty).Trim().TrimEnd('/'),
            (right ?? string.Empty).Trim().TrimEnd('/'),
            StringComparison.OrdinalIgnoreCase
        );
    }

    private static string FormatDatasource(string server, string database)
    {
        return $"Server={server}; Database={database}";
    }

    private async Task<ReportDefinitionResponse> GetReportDefinitionAsync(
        string workspaceId,
        string reportId)
    {
        return await GetDefinitionAsync(
            $"workspaces/{workspaceId}/reports/{reportId}/getDefinition"
        );
    }

    private async Task<ReportDefinitionResponse> GetPaginatedDefinitionAsync(
        string workspaceId,
        string reportId)
    {
        ReportDefinitionResponse response = await GetDefinitionAsync(
            $"workspaces/{workspaceId}/paginatedReports/{reportId}/getDefinition"
        );

        if (string.IsNullOrWhiteSpace(response.Definition.Format))
        {
            response.Definition.Format = "PaginatedReportDefinition";
        }

        return response;
    }

    private async Task<ReportDefinitionResponse> GetDefinitionAsync(string endpoint)
    {
        using HttpResponseMessage response = await _fabricApiClient.PostAsync(endpoint);

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            string json = await response.Content.ReadAsStringAsync();
            ReportDefinitionResponse result = JsonSerializer.Deserialize<ReportDefinitionResponse>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            return result ?? throw new InvalidOperationException("Fabric returned an empty definition.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
        {
            string operationId = GetOperationId(response);
            await WaitForOperationAsync(operationId, GetRetryAfter(response));

            ReportDefinitionResponse result =
                await _fabricApiClient.GetAsync<ReportDefinitionResponse>(
                    $"operations/{operationId}/result"
                );

            return result ?? throw new InvalidOperationException("Fabric returned an empty operation result.");
        }

        string error = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException(
            $"Error getting item definition. HTTP {(int)response.StatusCode}: {error}"
        );
    }

    private async Task UpdateDefinitionAsync<TRequest>(string endpoint, TRequest request)
    {
        using HttpResponseMessage response = await _fabricApiClient.PostResponseAsync(endpoint, request);

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            return;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
        {
            string operationId = GetOperationId(response);
            await WaitForOperationAsync(operationId, GetRetryAfter(response));
            return;
        }

        string error = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException(
            $"Error updating item definition. HTTP {(int)response.StatusCode}: {error}"
        );
    }

    private async Task WaitForOperationAsync(string operationId, int retryAfter)
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, retryAfter)));

            FabricOperation operation = await _fabricApiClient.GetAsync<FabricOperation>(
                $"operations/{operationId}"
            );

            if (operation == null)
            {
                throw new InvalidOperationException("Fabric returned an empty operation response.");
            }

            if (string.Equals(operation.Status, "Succeeded", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.Equals(operation.Status, "Failed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(operation.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Fabric operation finished with status: {operation.Status}. Error: {operation.Error}"
                );
            }

            retryAfter = 2;
        }
    }

    private static string GetOperationId(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("x-ms-operation-id", out IEnumerable<string> values))
        {
            return values.First();
        }

        if (response.Headers.Location != null)
        {
            string id = response.Headers.Location.ToString().TrimEnd('/').Split('/').Last();

            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }
        }

        throw new InvalidOperationException("Fabric did not return x-ms-operation-id.");
    }

    private static int GetRetryAfter(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Retry-After", out IEnumerable<string> values) &&
            int.TryParse(values.FirstOrDefault(), out int seconds))
        {
            return seconds;
        }

        return 2;
    }

    private static T FindUniqueByName<T>(
        IEnumerable<T> items,
        string displayName,
        string artifactType,
        string workspaceName) where T : class
    {
        List<T> matches = items.Where(x =>
        {
            string name = x switch
            {
                Report report => report.DisplayName,
                PaginatedReport report => report.DisplayName,
                SemanticModel model => model.DisplayName,
                _ => string.Empty
            };

            return string.Equals(name, displayName, StringComparison.OrdinalIgnoreCase);
        }).ToList();

        if (matches.Count == 0)
        {
            throw new InvalidOperationException(
                $"The {artifactType} '{displayName}' was not found in workspace '{workspaceName}'."
            );
        }

        if (matches.Count > 1)
        {
            throw new InvalidOperationException(
                $"More than one {artifactType} named '{displayName}' exists in workspace '{workspaceName}'."
            );
        }

        return matches[0];
    }

    private static string ExtractSemanticModelId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        Match semanticModelMatch = Regex.Match(
            value,
            @"(?i)semanticmodelid\s*=\s*([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})"
        );

        if (semanticModelMatch.Success)
        {
            return semanticModelMatch.Groups[1].Value;
        }

        Match virtualServerMatch = Regex.Match(
            value,
            @"(?i)sobe_wowvirtualserver-([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})"
        );

        return virtualServerMatch.Success
            ? virtualServerMatch.Groups[1].Value
            : string.Empty;
    }

    private static string TrimLiteral(string value)
    {
        return (value ?? string.Empty).Trim().Trim('\'', '"');
    }

    private static string ReplaceIgnoreCase(string input, string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(oldValue))
        {
            return input;
        }

        return Regex.Replace(
            input,
            Regex.Escape(oldValue),
            _ => newValue,
            RegexOptions.IgnoreCase
        );
    }

    private static bool EqualsIgnoreCase(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }


    private static XDocument LoadXmlDocumentFromBase64(ReportDefinitionPart part)
    {
        if (string.IsNullOrWhiteSpace(part.Payload))
        {
            throw new InvalidOperationException(
                $"Fabric returned an empty payload for RDL part '{part.Path}'."
            );
        }

        byte[] bytes;

        try
        {
            bytes = Convert.FromBase64String(part.Payload);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                $"The RDL payload is not valid Base64. Part: '{part.Path}'. Payload type: '{part.PayloadType}'.",
                ex
            );
        }

        try
        {
            using MemoryStream stream = new(bytes, writable: false);

            // Parse from the original byte stream instead of first converting it to a .NET string.
            // This lets the XML parser consume UTF-8/UTF-16 BOMs and honor the encoding declared
            // by the RDL document. Converting a UTF-8 BOM to U+FEFF and then calling
            // XDocument.Parse can cause: "Data at the root level is invalid. Line 1, position 1."
            return XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        }
        catch (XmlException ex)
        {
            throw new InvalidOperationException(
                $"The RDL part '{part.Path}' could not be parsed as XML. " +
                $"Payload type: '{part.PayloadType}'. Decoded prefix: {GetDecodedPrefix(bytes)}",
                ex
            );
        }
    }

    private static string GetDecodedPrefix(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return "(empty)";
        }

        int length = Math.Min(bytes.Length, 120);
        string text;

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            length -= length % 2;
            text = Encoding.Unicode.GetString(bytes, 0, length);
        }
        else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            length -= length % 2;
            text = Encoding.BigEndianUnicode.GetString(bytes, 0, length);
        }
        else
        {
            text = Encoding.UTF8.GetString(bytes, 0, length);
        }

        return text
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();
    }

    private static string EncodeXmlDocumentBase64(XDocument document)
    {
        using MemoryStream stream = new();
        XmlWriterSettings settings = new()
        {
            Encoding = new UTF8Encoding(false),
            Indent = false,
            OmitXmlDeclaration = false
        };

        using (XmlWriter writer = XmlWriter.Create(stream, settings))
        {
            document.Save(writer);
        }

        return Convert.ToBase64String(stream.ToArray());
    }

    private static string DecodeBase64Utf8(string payload)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(payload));
    }

    private static string EncodeBase64Utf8(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    private static JsonSerializerOptions JsonIndentedOptions()
    {
        return new JsonSerializerOptions { WriteIndented = true };
    }

    private static string CreateBackupDirectory(FixRdlAnalysis analysis)
    {
        string directory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "backup",
            $"{DateTime.Now:yyyyMMdd-HHmmss}-{SanitizeFileName(analysis.TargetWorkspace.DisplayName)}-{SanitizeFileName(analysis.TargetReport.DisplayName)}"
        );

        Directory.CreateDirectory(directory);
        return directory;
    }

    private static async Task SaveBackupAsync(
        string directory,
        string fileName,
        ReportDefinitionResponse definition)
    {
        string json = JsonSerializer.Serialize(
            definition,
            new JsonSerializerOptions { WriteIndented = true }
        );

        await File.WriteAllTextAsync(
            Path.Combine(directory, fileName),
            json,
            Encoding.UTF8
        );
    }

    private static string SanitizeFileName(string value)
    {
        string result = value ?? string.Empty;

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            result = result.Replace(invalidChar, '-');
        }

        return result.Trim();
    }
}
