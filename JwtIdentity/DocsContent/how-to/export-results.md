# Export Survey Results

## Prepare your data
Filter responses by date range or segment, choose whether to include partial responses, and rename columns before exporting.

## Start the export
Pick a format (CSV or Excel), review the filters and question set, and generate the export. SurveyShark queues the job and emails you when it is ready.

## Download formats
- **CSV** — best for ETL pipelines and lightweight analysis.
- **Excel** — includes formatting, multiple worksheets, and pivot-ready tables.
- **JSON** — ideal for automation and system integrations.

## Automation tips
Use the `/api/surveys/{id}/export` endpoint for scheduled exports, retain downloads for up to 30 days, and encrypt files stored in your organization’s systems.
