# Bar Chart Component

## Component overview
The Bar Chart component wraps MudBlazor's `MudChart` with responsive sizing, theming, and tooltip support for analytics dashboards.

## Configuration options
- Bind chart data and labels to API-driven response counts.
- Provide descriptive axis labels and format values with percentages or currency symbols.
- Customize palettes or switch to horizontal bars for lengthy labels.

## Accessibility guidance
Offer text summaries beneath each chart, maintain a 4.5:1 contrast ratio, and ensure keyboard navigation reaches legends and tooltips.

## Troubleshooting tips
- Empty charts usually indicate missing data or an uninitialized `ChartData` collection.
- Unexpected tooltip output often stems from formatting callbacks that do not handle `null` values.
- For mobile layout issues, wrap the chart in a padded container and allow horizontal scrolling for wide legends.
