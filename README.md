# garageos-execucao-service

Microsserviço **Execução** do GarageOS — Tech Challenge Fase 4.

Dono do estoque de peças, da reserva de peças por OS, da fila de execução da oficina (diagnóstico e reparo) e do relatório de aging.

| | |
|---|---|
| Banco | Amazon RDS PostgreSQL 16 (instância própria) |
| Mensageria | Amazon SQS/SNS com MassTransit 8.x (etapa 5) |
| Papel na saga | Participante: reserva, libera e consome peças; executa a OS |
| Namespace no EKS | `garageos-execucao` |

As decisões de arquitetura estão nos ADRs 0004 a 0007 em
[`garageos-os-service/docs/adrs`](https://github.com/TechChallengeFase1/garageos-os-service/tree/main/docs/adrs).

## Rotas

Todas exigem JWT com role `Admin` (login administrativo do OS Service).

| Método | Rota | Descrição |
|---|---|---|
| `GET` `POST` | `/api/estoques` | Lista e cadastra peças |
| `GET` `PUT` `DELETE` | `/api/estoques/{id}` | Consulta, altera e remove. O OS Service usa o `GET` para validar e precificar peças |
| `POST` | `/api/reservas` | Reserva as peças de uma OS: todas ou nenhuma (409 se faltar). Idempotente por OS |
| `GET` | `/api/reservas/{ordemDeServicoId}` | Reserva de uma OS |
| `POST` | `/api/reservas/{ordemDeServicoId}/liberar` | Compensação: devolve as peças ao disponível |
| `POST` | `/api/reservas/{ordemDeServicoId}/consumir` | Baixa definitiva quando a execução termina |
| `POST` | `/api/execucoes` | Coloca uma OS na fila. Idempotente por OS |
| `GET` | `/api/execucoes?status=` | Fila na ordem de atendimento |
| `GET` | `/api/execucoes/{id}` | Execução com serviços e histórico |
| `PATCH` | `/api/execucoes/{id}/status` | NaFila → EmDiagnostico → EmReparo → Concluida, ou Falhou (com motivo) |
| `PATCH` | `/api/execucoes/{id}/itens/{itemId}/status` | Inicia ou finaliza um serviço |
| `GET` | `/api/execucoes/aging` | Tempo médio por serviço, agregado no banco |
| `GET` | `/health/live` · `/health/ready` | Health checks |

### Peças em dois tempos

A peça não sai do estoque na aprovação. Ela é **reservada** (continua no estoque, mas indisponível para outras OS) e só é **consumida** quando a execução termina. Se o pagamento falhar no meio, a reserva é **liberada** e nada se perdeu. Concorrência protegida pelo `xmin` do PostgreSQL: duas reservas simultâneas da mesma peça não ultrapassam o saldo.

## Como rodar localmente

```bash
cp .env.example .env   # preencha banco e JWT
docker compose up -d --build
```

Swagger em <http://localhost:8082/swagger>.

## Testes

```bash
dotnet test Code/GarageOS.Execucao.UnitTests           # sem dependências
dotnet test Code/GarageOS.Execucao.IntegrationTests    # precisa de Docker (Testcontainers)
```

O CI ([`ci.yml`](.github/workflows/ci.yml)) roda os dois a cada PR.

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
