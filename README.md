# garageos-execucao-service

Microsservico **Execução Service** do GarageOS — Tech Challenge Fase 4.

Estoque e reserva de pecas, fila de execucao da OS, status de diagnostico e reparo, aging.

| | |
|---|---|
| Banco | Amazon RDS PostgreSQL 16 (instancia propria) |
| Mensageria | Amazon SQS/SNS com MassTransit 8.x |
| Papel na saga | Participante — ver ADR 0005 no `garageos-app` |
| Namespace no EKS | `garageos-execucao` |

> Repositório em construção. As decisões de arquitetura estão nos ADRs 0004 a 0007 em
> [`garageos-app/docs/adrs`](https://github.com/TechChallengeFase1/garageos-app/tree/main/docs/adrs).

## Infraestrutura (`infra/`)

Terraform com o que é só deste serviço: repositório ECR, banco, role IRSA dos pods e
parâmetros SSM em `/garageos/producao/services/execucao/*`. O state fica em
`services/execucao/terraform.tfstate`, no bucket compartilhado.

Pré-requisitos, nesta ordem:

1. Este repositório listado em `github_repos` no bootstrap (`garageos-infra-database/bootstrap`), com o apply feito.
2. Cluster no ar pelo `garageos-infra-k8s`, que publica o OIDC do cluster e o namespace deste serviço.
3. `garageos-infra-database` aplicado, que publica o crachá `rds-client`.

O workflow [`infra.yml`](.github/workflows/infra.yml) faz plan no PR e apply no merge na `main`.
Depois de recriar o cluster, reaplique: a trust da role IRSA aponta para o OIDC do cluster, que muda.
