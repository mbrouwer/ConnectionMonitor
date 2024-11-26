resource "azurerm_user_assigned_identity" "identity" {
  location            = azurerm_resource_group.resourceGroup.location
  name                = "uai-connectionmonitor"
  resource_group_name = azurerm_resource_group.resourceGroup.name
}