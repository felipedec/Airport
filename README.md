# Como usar:

Quando executar a simulação pressione a tecla `Home` ou a tecla `Tab` para abrir o **Modo Console**, aonde o sera capaz de executar comandos para controlar a simulação, digite o comando `help` para saber quais comandos estão disponíveis.

## Executar Arquivo:

Com o objetivo de agrupar grupos de comandos para serem utilizados proceduralmente, crie um arquivo `.txt` e escreva os comandos que deseja executar, para executar o arquivo basta utilizar o comando `exec <nome_do_arquivo>` (note que a extensão do arquivo é opcional).

Se quiser executar os cenários de exemplo disponibilizados nas pastas Scenarios ou Cenarios, você pode salvar as pastas Scenarios, Cenarios, Aircrafts e Aeronaves em seu Desktop e executar o comando `exec %Desktop%\Scenarios\<nome_do_cenario>` ou `exec %Desktop%\Cenarios\<nome_do_cenario>`. Depois disso, digite comando `Home` e o simulador vai começar a rodar o cenário.

## Como Criar uma Aeronave:

Para criar uma aeronave deve se seguir o seguinte padrão:

```
# criar aeronave e atribuir o valor do seu transponder a %t%
create_aircraft
set t %cvar:aircraft_max_transponder%

# utilizar o transponder %t% para atribuir os campos da aeronave
aircraft_set_field %t% 	weight 13E3
aircraft_set_field %t% 	fuel 8000
aircraft_set_field %t% 	origin "Porto Alegre"
aircraft_set_field %t% 	fuelovertime 800
aircraft_set_field %t% 	altitude 	50
aircraft_set_field %t% 	priority 	17
aircraft_set_field %t% 	flightid 	60

# definir o estado que a aeronave se encontra
aircraft_set_state %t% taxiwayleaving
```

# Sobre:

Estre projeto foi desenvolvido para a cadeira de Modelagem e Simulação do Curso de Ciência da Computação (UFN) por Felipe Marques, Keliton Amaral e João Victor.
