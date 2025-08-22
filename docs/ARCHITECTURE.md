
### Contexto
![C4 Context](./docs/architecture/C4-Context.png)

## Containers (do README)
![C4 Container](./architecture/C4-Container.png)

## Estrutura de Eventos (do README)
- Evento publicado: `LancamentoCreated` (extensível para Updated/Deleted)
- Exchange: `lancamentos` (fanout)
- Fila consumer: `lancamentos_consolidacao`

## Modelo de Dados Essencial (do README)
`Lancamento`: Id, FluxoDeCaixaId, Valor, Tipo (0 crédito / 1 débito), Data (UTC), Descricao
`SaldoDiario`: Data (PK), SaldoTotal
