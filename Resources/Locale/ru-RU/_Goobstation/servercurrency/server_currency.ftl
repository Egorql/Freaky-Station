# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
# SPDX-FileCopyrightText: 2025 SX-7 <92227810+SX-7@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

server-currency-name-singular = Фрики Коин
server-currency-name-plural = Фрики Коины

## Команды

server-currency-gift-command = подарить
server-currency-gift-command-description = Передать часть вашего баланса другому игроку.
server-currency-gift-command-help = Использование: подарить <игрок> <сумма>
server-currency-gift-command-error-1 = Вы не можете подарить себе!
server-currency-gift-command-error-2 = У вас недостаточно средств для передачи! Ваш баланс: { $balance }.
server-currency-gift-command-giver = Вы передали { $player } { $amount }.
server-currency-gift-command-reciever = { $player } передал вам { $amount }.
server-currency-balance-command = баланс
server-currency-balance-command-description = Показывает ваш баланс.
server-currency-balance-command-help = Использование: баланс
server-currency-balance-command-return = У вас { $balance }.
server-currency-add-command = баланс:добавить
server-currency-add-command-description = Добавляет валюту на счет игрока.
server-currency-add-command-help = Использование: баланс:добавить <игрок> <сумма>
server-currency-remove-command = баланс:убрать
server-currency-remove-command-description = Убирает валюту со счета игрока.
server-currency-remove-command-help = Использование: баланс:убрать <игрок> <сумма>
server-currency-set-command = баланс:установить
server-currency-set-command-description = Устанавливает баланс игрока.
server-currency-set-command-help = Использование: баланс:установить <игрок> <сумма>
server-currency-get-command = баланс:узнать
server-currency-get-command-description = Узнаёт баланс указанного игрока.
server-currency-get-command-help = Использование: баланс:узнать <игрок>
server-currency-command-completion-1 = Имя игрока
server-currency-command-completion-2 = Значение
server-currency-command-error-1 = Игрок с таким именем не найден.
server-currency-command-error-2 = Значение должно быть целым числом.
server-currency-command-return = У { $player } { $balance }.

# Обновление 65%

gs-balanceui-title = Магазин
gs-balanceui-confirm = Подтвердить
gs-balanceui-gift-label = Перевод:
gs-balanceui-gift-player = Игрок
gs-balanceui-gift-player-tooltip = Введите имя игрока, которому хотите отправить деньги
gs-balanceui-gift-value = Сумма
gs-balanceui-gift-value-tooltip = Количество денег для перевода
gs-balanceui-shop-label = Магазин токенов
gs-balanceui-shop-empty = Нет в наличии!
gs-balanceui-shop-buy = Купить
gs-balanceui-shop-footer = ⚠ Используйте ваш токен через Ahelp. Только 1 раз в день.
gs-balanceui-shop-token-label = Токены
gs-balanceui-shop-tittle-label = Титулы
gs-balanceui-shop-buy-token-antag-high = Купить хайтир токен антага - { $price } Фрики Коинов
gs-balanceui-shop-buy-token-antag-mid = Купить мидтир токен антага - { $price } Фрики Коинов
gs-balanceui-shop-buy-token-antag-low = Купить лоутир токен антага - { $price } Фрики Коинов
gs-balanceui-shop-buy-token-admin-abuse = Купить токен на злоупотребление админом - { $price } Фрики Коинов
gs-balanceui-shop-buy-token-hat = Купить токен на шляпу - { $price } Фрики Коинов
gs-balanceui-shop-token-antag-high = Хайтир токен антага
gs-balanceui-shop-token-antag-mid = Мидтир токен антага
gs-balanceui-shop-token-antag-low = Лоутир токен антага
gs-balanceui-shop-token-admin-abuse = Токен злоупотребления админом
gs-balanceui-shop-token-hat = Токен шляпы
gs-balanceui-shop-buy-token-antag-high-desc = Позволяет получить роль хайтир антага (кроме волшебников).
gs-balanceui-shop-buy-token-antag-mid-desc = Позволяет получить роль мидтир антага.
gs-balanceui-shop-buy-token-antag-low-desc = Позволяет получить роль лоутир антага.
gs-balanceui-shop-buy-token-admin-abuse-desc = Позволяет попросить админа злоупотребить своими полномочиями по отношению к вам. Админам рекомендуется не сдерживаться.
gs-balanceui-shop-buy-token-hat-desc = Админ выдаст вам случайную шляпу.
gs-balanceui-admin-add-label = Добавить (или убрать) деньги:
gs-balanceui-admin-add-player = Имя игрока
gs-balanceui-admin-add-value = Сумма
gs-balanceui-remark-token-antag-high = Куплен хайтир токен антага.
gs-balanceui-remark-token-antag-mid = Куплен мидтир токен антага.
gs-balanceui-remark-token-antag-low = Куплен лоутир токен антага.
gs-balanceui-remark-token-admin-abuse = Куплен токен злоупотребления админом.
gs-balanceui-remark-token-hat = Куплен токен шляпы.
gs-balanceui-shop-click-confirm = Нажмите ещё раз для подтверждения
gs-balanceui-shop-purchased = Куплено { $item }
gs-balanceui-roulette-label = Рулетка Фрики Коинов
gs-balanceui-roulette-open = Открыть рулетку
gs-balanceui-roulette-bet = Ставка
gs-balanceui-roulette-spin = Крутить
gs-balanceui-roulette-balance = Баланс: { $balance }
gs-balanceui-roulette-spinning = Крутим...
gs-balanceui-roulette-invalid-bet = Введите корректную ставку (целое число не меньше { $min }).
gs-balanceui-roulette-mode-mult = Режимы коэффициента
gs-balanceui-roulette-mode-x2 = x2
gs-balanceui-roulette-mode-x5 = x5
gs-balanceui-roulette-mode-x10 = x10
gs-balanceui-roulette-mode-selected = Режим: { $mode } | Шанс выигрыша: { $chance }% | Коэффициент: x{ $multiplier }
gs-balanceui-roulette-fast-spin = Быстрый спин
gs-balanceui-roulette-result-lose = Рулетка: x{ $multiplier }. Вы депнули { $bet }.
gs-balanceui-roulette-result-win = Рулетка: x{ $multiplier }. Ставка { $bet } -> выплата { $payout }.
gs-roulette-jackpot-notify = { $player } сорвал ДЖЕКПОТ x10 и получил { $amount }! Всего депнуто в рулетку на сервере: { $pot }.
gs-roulette-busted-notify = { $player } депнул все свои Фрики Коины в рулетке. Лошара
gs-roulette-round-end-lost = В рулетке за раунд проиграно: { $amount }.
gs-roulette-station-report-lost = Рулетка: за раунд проиграно { $amount }.
