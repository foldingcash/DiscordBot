Stop-Service -Name "FoldingCashDiscordBot"

Remove-Service -Name "FoldingCashDiscordBot"

sc.exe delete "FoldingCashDiscordBot"