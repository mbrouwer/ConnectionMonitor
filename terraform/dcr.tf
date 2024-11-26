resource "azurerm_monitor_data_collection_endpoint" "dataCollectionEndpoint" {
  name                          = "dceconnectionmonitor"
  resource_group_name           = azurerm_resource_group.resourceGroup.name
  location                      = azurerm_resource_group.resourceGroup.location
  public_network_access_enabled = true
}

resource "azurerm_monitor_data_collection_rule" "dataCollectionRuleconnmon" {
  name                        = "dcrconnectionmonitor"
  resource_group_name         = azurerm_resource_group.resourceGroup.name
  location                    = azurerm_resource_group.resourceGroup.location
  data_collection_endpoint_id = azurerm_monitor_data_collection_endpoint.dataCollectionEndpoint.id

  destinations {
    log_analytics {
      workspace_resource_id = azurerm_log_analytics_workspace.logAnalytics.id
      name                  = azurerm_log_analytics_workspace.logAnalytics.workspace_id
    }
  }
  data_flow {
    streams       = ["Custom-ConnMon_CL"]
    destinations  = [azurerm_log_analytics_workspace.logAnalytics.workspace_id]
    output_stream = "Custom-ConnMon_CL"
    transform_kql = "source | extend jsonContext = parse_json(AdditionalContext) | project TimeGenerated, Computer, Status, Host, Protocol, Port, AdditionalContext = jsonContext"
  }

  stream_declaration {
    stream_name = "Custom-ConnMon_CL"
    column {
      name = "TimeGenerated"
      type = "datetime"
    }
    column {
      name = "Computer"
      type = "string"
    }
    column {
      name = "Status"
      type = "string"
    }
    column {
      name = "Host"
      type = "string"
    }
    column {
      name = "Protocol"
      type = "string"
    }
    column {
      name = "Port"
      type = "int"
    }
    column {
      name = "AdditionalContext"
      type = "string"
    }
  }
  depends_on = [time_sleep.workspace_create]
}


resource "azurerm_role_assignment" "logAnalytics" {
  scope                = azurerm_monitor_data_collection_rule.dataCollectionRuleconnmon.id
  role_definition_name = "Monitoring Metrics Publisher"
  principal_id         = azurerm_user_assigned_identity.identity.principal_id
}
