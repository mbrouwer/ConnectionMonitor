resource "azurerm_log_analytics_workspace" "logAnalytics" {
  name                = var.logAnalyticsName
  location            = azurerm_resource_group.resourceGroup.location
  resource_group_name = azurerm_resource_group.resourceGroup.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "time_sleep" "workspace_create" {
  create_duration = "120s"

  triggers = {
    workspace_trigger = azurerm_log_analytics_workspace.logAnalytics.id
  }
}

resource "azapi_resource" "createtableconn" {
  type      = "Microsoft.OperationalInsights/workspaces/tables@2022-10-01"
  name      = "ConnMon_CL"
  parent_id = azurerm_log_analytics_workspace.logAnalytics.id

  body = jsonencode({
    properties = {
      "plan" : "Analytics",
      "retentionInDays" : 30,
      "totalRetentionInDays" : 365,
      "schema" : {
        "name" : "ConnMon_CL",
        "columns" : [{
          "name" : "TimeGenerated"
          "type" : "datetime"
          },
          {
            "name" : "Computer"
            "type" : "string"
          },
          {
            "name" : "Status"
            "type" : "string"
          },
          {
            "name" : "Host"
            "type" : "string"
          },
          {
            "name" : "Protocol"
            "type" : "string"
          },
          {
            "name" : "Port"
            "type" : "int"
          },
          {
            "name" : "AdditionalContext"
            "type" : "dynamic"
          }
        ]
      }
    }
  })
  depends_on = [time_sleep.workspace_create]
}
