locals {
  allowed_environments             = ["nonproduction"]
  resource_prefix                  = "bluto-${var.environment}"
  api_workload_identity_name       = "${local.resource_prefix}-api-identity"
  worker_workload_identity_name    = "${local.resource_prefix}-worker-identity"
  migration_workload_identity_name = "${local.resource_prefix}-migration-identity"
  key_vault_name                   = "${local.resource_prefix}-vault"
  key_vault_reference              = "kvref://${local.key_vault_name}/runtime-configuration"
  postgres_flexible_server_name    = "${local.resource_prefix}-postgres"
  container_registry_name          = replace("${local.resource_prefix}-acr", "-", "")
  log_analytics_workspace_name     = "${local.resource_prefix}-logs"
  container_apps_environment       = "${local.resource_prefix}-apps"
}

locals {
  deployment_contract = {
    environment                   = var.environment
    region                        = var.region
    api_image_digest              = var.api_image_digest
    worker_image_digest           = var.worker_image_digest
    api_workload_identity_name    = local.api_workload_identity_name
    worker_workload_identity_name = local.worker_workload_identity_name
    key_vault_reference           = local.key_vault_reference
    public_network_access_enabled = var.public_network_access_enabled
    expected_azure_resources = [
      "resource_group",
      "container_registry",
      "log_analytics_workspace",
      "container_apps_environment",
      "api_container_app",
      "worker_container_job",
      "postgresql_flexible_server",
      "key_vault",
      "private_endpoints"
    ]
  }
}