data "azurerm_container_registry" "acr" {
  name                = "cloudwarrior"
  resource_group_name = "containerregistries"
}