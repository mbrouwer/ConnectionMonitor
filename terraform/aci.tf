data "azurerm_storage_account" "storageAccount" {
  name                = "cloudwarrior"
  resource_group_name = "general"
}

data "azurerm_virtual_network" "vnet" {
  name                = "vnet-main"
  resource_group_name = "network"
}

resource "azurerm_subnet" "subnet" {
  name                 = "aci"
  resource_group_name  = "network"
  virtual_network_name = data.azurerm_virtual_network.vnet.name
  address_prefixes     = ["10.1.4.0/24"]
  delegation {
    name = "aci"
    service_delegation {
      name    = "Microsoft.ContainerInstance/containerGroups"
      actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
    }
  }
}

resource "azurerm_container_group" "aci" {
  name                = "connectionmonitor"
  resource_group_name = azurerm_resource_group.resourceGroup.name
  location            = azurerm_resource_group.resourceGroup.location
  os_type             = "Linux"
  restart_policy      = "OnFailure"
  ip_address_type     = "Private"
  subnet_ids          = [azurerm_subnet.subnet.id]

  container {
    name   = "connectionmonitor"
    image  = "cloudwarrior.azurecr.io/connectionmonitor:latest"
    cpu    = 1
    memory = 1.5
    environment_variables = {
      dataCollectionRuleImmutableId   = azurerm_monitor_data_collection_rule.dataCollectionRuleconnmon.immutable_id
      dataCollectionRulelogsIngestion = azurerm_monitor_data_collection_endpoint.dataCollectionEndpoint.logs_ingestion_endpoint
    }

    volume {
      name                 = "connmon"
      mount_path           = "/app/config"
      storage_account_name = data.azurerm_storage_account.storageAccount.name
      share_name           = "connmon"
      storage_account_key  = data.azurerm_storage_account.storageAccount.primary_access_key
    }

    ports {
      port     = 80
      protocol = "TCP"
    }

  }

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.identity.id]
  }

  image_registry_credential {
    server   = data.azurerm_container_registry.acr.login_server
    username = data.azurerm_container_registry.acr.admin_username
    password = data.azurerm_container_registry.acr.admin_password
  }
}
