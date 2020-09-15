$Name = "FoldingCashDiscordBot"

Stop-Service -Name $Name

Remove-Service -Name $Name

sc.exe delete $Name