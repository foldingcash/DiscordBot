$params = @{
  Name = "FoldingCashDiscordBot"
  DisplayName = "Folding Cash Discord Bot"
  Description = "This is a bot for FoldingCash's Discord server."
  BinaryPathName = "dotnet C:\DiscordBot\DiscordBot.Console.dll --environment=production"
  StartupType = "Automatic"
}
New-Service @params

Start-Service -Name "FoldingCashDiscordBot"