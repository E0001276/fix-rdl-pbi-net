# fix-rdl-pbi-net

Windows Forms application built with .NET 9 to remediate Power BI/Fabric links after promoting reports between workspaces.

## Fix RDL Visual

Open the form from **Fabric > Fix RDL Visual**.

The remediation can:
- Rebind the target Power BI report to the target semantic model.
- Recreate each PBIR RDL visual reference as a canonical `ItemLocation` reference using the target workspace ID and the target paginated report item ID.
- Verify the RDL visual references again after Fabric persists the updated report definition. The operation fails if Fabric does not persist the expected target IDs.
- Rebind paginated-report runtime data sources to the target semantic model using the Power BI REST API.
- Bind the semantic model to the matching gateway when required.

## Forced RDL relink

If **Analyze** reports zero changes but an RDL visual still does not render or Power BI asks you to select the paginated report again, **Apply fixes remains enabled**.

In that case the application performs a forced RDL relink:
1. It recreates the complete `visual.objects.reportInfo[0].properties.reference` object.
2. It writes `kind = ItemLocation`.
3. It writes the target `workspaceId` and target paginated-report `itemId` as literal expressions.
4. It sends the report definition to Fabric.
5. It downloads the persisted definition again and verifies each page against the expected target paginated report.

This is intentionally stronger than only replacing GUID strings in the existing JSON.

## Safety

Only the target workspace is modified. Before applying changes, the current target definitions are saved under the local `backup` folder.

The project targets `net9.0-windows` and uses `<Nullable>disable</Nullable>`.

## Diagnostic logging for Apply fixes

When Apply fixes runs, the application creates a diagnostic session under:

`<application folder>/diagnostics/yyyyMMdd-HHmmss-<workspace>-<report>/`

For paginated report definition updates it records the definition before and after the in-memory change, decoded RDL XML, Base64 payloads, the exact updateDefinition request, HTTP response headers/body, Fabric long-running operation states, the definition returned during verification, a summary, and the complete exception when a failure occurs.

Authorization tokens and credentials are not written to diagnostic files. If Apply fixes fails, the diagnostics directory is shown in the execution log and error dialog.
