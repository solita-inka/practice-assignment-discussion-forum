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