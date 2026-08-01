variable "environment" {
  type        = string
  description = "Deployment environment. WP-012 is limited to nonproduction."
  default     = "nonproduction"

  validation {
    condition     = contains(local.allowed_environments, var.environment)
    error_message = "WP-012 only permits nonproduction deployment configuration."
  }
}

variable "region" {
  type        = string
  description = "Azure region for nonproduction resources."
  default     = "eastus2"
}

variable "api_image_digest" {
  type        = string
  description = "Immutable API image digest supplied by CI."
  default     = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
}

variable "worker_image_digest" {
  type        = string
  description = "Immutable worker image digest supplied by CI."
  default     = "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
}

variable "public_network_access_enabled" {
  type        = bool
  description = "Database public network access must remain disabled."
  default     = false

  validation {
    condition     = var.public_network_access_enabled == false
    error_message = "Public database endpoints are prohibited by WP-012."
  }
}