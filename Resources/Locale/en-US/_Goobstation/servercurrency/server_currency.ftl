# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
# SPDX-FileCopyrightText: 2025 SX-7 <92227810+SX-7@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

server-currency-name-singular = Goob Coin
server-currency-name-plural = Goob Coins

## Commands

server-currency-gift-command = gift
server-currency-gift-command-description = Gifts some of your balance to another player.
server-currency-gift-command-help = Usage: gift <player> <value>
server-currency-gift-command-error-1 = You can't gift yourself!
server-currency-gift-command-error-2 = You can not afford to gift this! You have a balance of {$balance}.
server-currency-gift-command-giver = You gave {$player} {$amount}.
server-currency-gift-command-reciever = {$player} gave you {$amount}.

server-currency-balance-command = balance
server-currency-balance-command-description = Returns your balance.
server-currency-balance-command-help = Usage: balance
server-currency-balance-command-return = You have {$balance}.

server-currency-add-command = balance:add
server-currency-add-command-description = Adds currency to a player's balance.
server-currency-add-command-help = Usage: balance:add <player> <value>

server-currency-remove-command = balance:rem
server-currency-remove-command-description = Removes currency from a player's balance.
server-currency-remove-command-help = Usage: balance:rem <player> <value>

server-currency-set-command = balance:set
server-currency-set-command-description = Sets a player's balance.
server-currency-set-command-help = Usage: balance:set <player> <value>

server-currency-get-command = balance:get
server-currency-get-command-description = Gets the balance of a player.
server-currency-get-command-help = Usage: balance:get <player>

server-currency-command-completion-1 = Username
server-currency-command-completion-2 = Value
server-currency-command-error-1 = Unable to find a player by that name.
server-currency-command-error-2 = Value must be an integer.
server-currency-command-return = {$player} has {$balance}.

# 65% Update

gs-balanceui-title = Store
gs-balanceui-confirm = Confirm

gs-balanceui-gift-label = Transfer:
gs-balanceui-gift-player = Player
gs-balanceui-gift-player-tooltip = Insert the name of the player you want to send the money to
gs-balanceui-gift-value = Value
gs-balanceui-gift-value-tooltip = Amount of money to transfer

gs-balanceui-shop-label = Tokens Store
gs-balanceui-shop-empty = Out of stock!
gs-balanceui-shop-buy = Buy
gs-balanceui-shop-footer = ⚠ Ahelp to use your token. Only 1 use per day.

gs-balanceui-shop-token-label = Tokens
gs-balanceui-shop-tittle-label = Titles

gs-balanceui-shop-buy-token-antag-high = Buy a High Tier Antag Token - {$price} Goob Coins
gs-balanceui-shop-buy-token-antag-mid = Buy a Mid Tier Antag Token - {$price} Goob Coins
gs-balanceui-shop-buy-token-antag-low = Buy a Low Tier Antag Token - {$price} Goob Coins
gs-balanceui-shop-buy-token-admin-abuse = Buy an Admin Abuse Token - {$price} Goob Coins
gs-balanceui-shop-buy-token-hat = Buy a Hat Token - {$price} Goob Coins

gs-balanceui-shop-token-antag-high = High Tier Antag Token
gs-balanceui-shop-token-antag-mid = Mid Tier Antag Token
gs-balanceui-shop-token-antag-low = Low Tier Antag Token
gs-balanceui-shop-token-admin-abuse = Admin Abuse Token
gs-balanceui-shop-token-hat = Hat Token

gs-balanceui-shop-buy-token-antag-high-desc = Allows a high-tier antag roll. (Excluding Wizards)
gs-balanceui-shop-buy-token-antag-mid-desc = Allows a mid-tier antag roll.
gs-balanceui-shop-buy-token-antag-low-desc = Allows a low-tier antag roll.
gs-balanceui-shop-buy-token-admin-abuse-desc = Allows you to request an admin to abuse their powers against you. Admins are encouraged to go wild.
gs-balanceui-shop-buy-token-hat-desc = An admin will give you a random hat.

gs-balanceui-admin-add-label = Add (or subtract) money:
gs-balanceui-admin-add-player = Player name
gs-balanceui-admin-add-value = Value

gs-balanceui-remark-token-antag-high = Bought a high tier antag token.
gs-balanceui-remark-token-antag-mid = Bought a mid tier antag token.
gs-balanceui-remark-token-antag-low = Bought a low tier antag token.
gs-balanceui-remark-token-admin-abuse = Bought an admin abuse token.
gs-balanceui-remark-token-hat = Bought a hat token.
gs-balanceui-shop-click-confirm = Click again to confirm
gs-balanceui-shop-purchased = Purchased {$item}
gs-balanceui-roulette-label = Freaky Coin Roulette
gs-balanceui-roulette-open = Open roulette
gs-balanceui-roulette-bet = Bet
gs-balanceui-roulette-spin = Spin
gs-balanceui-roulette-balance = Balance: {$balance}
gs-balanceui-roulette-spinning = Spinning...
gs-balanceui-roulette-invalid-bet = Enter a valid bet (integer not less than {$min}).
gs-balanceui-roulette-mode-mult = Coefficient modes
gs-balanceui-roulette-mode-x2 = x2
gs-balanceui-roulette-mode-x5 = x5
gs-balanceui-roulette-mode-x10 = x10
gs-balanceui-roulette-mode-selected = Mode: {$mode} | Win chance: {$chance}% | Multiplier: x{$multiplier}
gs-balanceui-roulette-fast-spin = Fast spin
gs-balanceui-roulette-result-lose = Roulette: x{$multiplier}. You lost {$bet}.
gs-balanceui-roulette-result-win = Roulette: x{$multiplier}. Bet {$bet} -> payout {$payout}.
gs-roulette-jackpot-notify = {$player} hit the x10 JACKPOT and won {$amount}! Total coins deposited into roulette server-wide: {$pot}.
gs-roulette-busted-notify = {$player} lost all their Freaky Coins in roulette.
gs-roulette-round-end-lost = Coins lost in roulette this round: {$amount}.
gs-roulette-station-report-lost = Roulette: coins lost this round {$amount}.
