variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "North Europe"
}

variable "owner_email" {
  description = "Owner tag for resources"
  type        = string
}

variable "due_date" {
  description = "Due date tag for resources"
  type        = string
}

variable "subscription_id" {
  description = "Azure subscription ID"
  type        = string
}

variable "sql_admin_password" {
  description = "SQL Server admin password"
  type        = string
  sensitive   = true
}

variable "app_service_plan_name" {
  description = "Name of the App Service Plan"
  type        = string
}

variable "storage_account_name" {
  description = "Name of the storage account for the function app"
  type        = string
}