$params = @{
  Name = "FoldingCashDiscordBot"
  DisplayName = "Folding Cash Discord Bot"
  Description = "This is a bot for FoldingCash's Discord server."
  BinaryPathName = "dotnet C:\DiscordBot\netcoreapp3.1\DiscordBot.Console.dll --environment=production"
  StartupType = "Automatic"
}
New-Service @params

Start-Service -Name "FoldingCashDiscordBot"