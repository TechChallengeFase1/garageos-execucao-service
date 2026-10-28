terraform {
  required_version = ">= 1.5.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  # Mesmo bucket dos repositorios da Fase 3, com uma chave por servico.
  # O state de um servico nunca enxerga os recursos de outro: um destroy
  # aqui so alcanca o que e deste servico (ADR 0004).
  backend "s3" {
    bucket       = "garageos-tfstate-266380777968"
    key          = "services/execucao/terraform.tfstate"
    region       = "us-east-1"
    encrypt      = true
    use_lockfile = true
  }
}

provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = var.project
      Environment = local.env
      Service     = local.service
      ManagedBy   = "terraform"
      Source      = "garageos-execucao-service"
    }
  }
}

locals {
  env     = terraform.workspace == "default" ? "producao" : terraform.workspace
  service = "execucao"

  # Prefixo de todo recurso deste servico: garageos-producao-execucao-*
  # As policies de IAM montam ARNs por esta convencao, sem precisar ler o
  # state de outro repositorio.
  name = "${var.project}-${local.env}-${local.service}"
}
