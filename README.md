# fix-rdl-pbi-net
Windows Forms application built with .NET 9 to fix Power BI paginated report visual references.

## Fix RDL Visual

The **Fabric > Fix RDL Visual** menu opens a remediation form for promoted Power BI reports that contain RDL visuals.

Workflow:
1. Select the source workspace (for example DEV).
2. Select the target workspace (for example QA).
3. Select the source Power BI report.
4. Click **Analyze**. The application matches the target Power BI report, semantic model, and paginated reports by display name and compares the actual item IDs in both workspaces.
5. Review the remediation plan.
6. Click **Apply fixes**. Before changing the target workspace, the application saves the current target item definitions under the local `backup` folder.

The remediation uses Microsoft Fabric REST APIs to update item definitions. It can:
- Rebind the target Power BI report to the target semantic model through `definition.pbir`.
- Rewrite RDL visual `workspaceId` and paginated report `itemId` references in PBIR visual definitions.
- Rewrite paginated report RDL connection strings so they point to the target semantic model, and update the embedded Power BI workspace/model metadata.

Only the target workspace is modified. Source workspace definitions are read-only inputs used to resolve source-to-target mappings.
