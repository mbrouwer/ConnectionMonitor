terraform {
  backend "azurerm" {
    # resource_group_name  = "storage"
    # storage_account_name = "cloudwarrior"
    # container_name       = "tfstate"
    # key                  = "terraform.tfstate"
  }
  required_providers {
    azurerm = {
      source = "hashicorp/azurerm"
    }
    azapi = {
      source = "Azure/azapi"
    }
    time = {
      source = "hashicorp/time"
    }
  }
}

provider "azurerm" {
  features {}
  subscription_id = "34b0c5b8-ee5b-48fa-a13f-a6424366d1b7"
}

variable "resourceGroupName" {
  type    = string
  default = "connectionmonitor-rg"
}

variable "resourceLocation" {
  type    = string
  default = "westeurope"
}

variable "logAnalyticsName" {
  type    = string
  default = "lawsconnectionmonitor"
}

resource "azurerm_resource_group" "resourceGroup" {
  name     = var.resourceGroupName
  location = var.resourceLocation
}

