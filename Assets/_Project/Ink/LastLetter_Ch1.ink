// =====================================================================
// «Остання адреса» — діалоги: пролог і глава 1
// Файл у репозиторії: Assets/Ink/LastLetter_Ch1.ink
// Веде: Яковець Софія (сценарій і Ink-діалоги, див. «Погодження» в GDD)
//
// Як влаштовано:
//  • Кожен вузол (=== назва ===) — одна розмова, яку Unity запускає
//    через DialogueManager.StartKnot("назва").
//  • Тег #speaker:… — хто говорить (для портрета й імені у вікні діалогу).
//  • Тег #give_item:… — Unity кладе предмет у сумку.
//  • Тег #emotion:… — вираз обличчя на портреті (neutral, smile, angry,
//    surprised, scared, sad, confused). Без тегу — neutral.
//  • Блок ТЕСТ унизу — лише для перевірки в Inky, гра його не запускає.
// =====================================================================

// --- Прапорці GameState з GDD («Програмний контент») ---
VAR prologue_done = false
VAR read_letter_fisher = false
VAR read_letter_tailor = false
VAR tailor_closed = false
VAR tailor_left = false

// --- Допоміжні змінні: узгодити з Артемом і дописати в GDD ---
VAR lantern_lit = false      // ліхтар біля мосту запалено (1-А, x = 26)
VAR intervention = 0         // GameState.intervention; +1 за читання додає сама гра
VAR test_mode = false        // true лише в тесті в Inky; Unity не змінює

-> test_start


// =====================================================================
// ПРОЛОГ — Площа Пошти (L1_PostSquare)
// =====================================================================

=== postmaster_intro ===
// Вступ: Ліля приходить до тями на Пошті (L1_PostSquare)
(Тиша. Десь дзенькають ключі. За прилавком хтось розглядає поштову марку крізь круглі окуляри.) #speaker:narrator
О. Прокинулась. #speaker:postmaster #emotion:smile
Де… де я? #speaker:lilia #emotion:scared
На Пошті, звісно. Де ж іще бути листоноші? #speaker:postmaster #emotion:smile
Я не листоноша. #speaker:lilia #emotion:confused
А сумка на тобі чия? #speaker:postmaster #emotion:smile
(Ліля дивиться вниз. Через плече — стара шкіряна сумка. Рукав кофти мокрий, і з нього досі капає.) #speaker:narrator
+ [Я не пам'ятаю, як сюди потрапила.]
    Я не пам'ятаю, як сюди потрапила. #speaker:lilia #emotion:sad
    Тут ніхто не пам'ятає. Повір, так легше. #speaker:postmaster #emotion:neutral
+ [Хто ви?]
    Хто ви? #speaker:lilia #emotion:confused
    Поштмейстер. Просто Поштмейстер. Імена тут швидко вицвітають — як чорнило на сонці. #speaker:postmaster #emotion:smile
+ [Я хочу додому.]
    Я хочу додому. #speaker:lilia #emotion:sad
    Усі хочуть. Але спершу — робота. #speaker:postmaster #emotion:neutral
- Попередня листоноша… пішла. Тож сумка тепер твоя. #speaker:postmaster #emotion:neutral
Куди пішла? #speaker:lilia #emotion:confused
Далі. #speaker:postmaster #emotion:smile
(Він кладе на прилавок конверт. Папір пожовклий, сургучна печатка ціла.) #speaker:narrator
Він… теплий. #speaker:lilia #emotion:surprised
Листи від живих завжди теплі. Не звикай. #speaker:postmaster #emotion:neutral
Правило одне, і воно просте: кожен лист має дійти до адресата. #speaker:postmaster #emotion:neutral
Запечатаним. #speaker:postmaster #emotion:smile
+ [А якщо я не знайду, кому він?]
    А якщо я не знайду, кому він? #speaker:lilia #emotion:scared
    Знайдеш. Листи завжди знаходять своїх. Іноді — надто пізно. #speaker:postmaster #emotion:neutral
+ [А можна подивитися, що там написано?]
    А можна… подивитися, що там написано? #speaker:lilia #emotion:neutral
    (Ключі на його поясі різко дзенькають.) #speaker:narrator
    Ні. #speaker:postmaster #emotion:angry
    Чужі слова — не для твоїх очей. Ніколи. Зрозуміла? #speaker:postmaster #emotion:angry
    Т-так… #speaker:lilia #emotion:scared
    (За мить він знову усміхається, ніби нічого не сталося.) #speaker:narrator
    От і славно. #speaker:postmaster #emotion:smile
- Твій перший лист. Адреса: «Тому, хто ловить там, де немає води». #speaker:postmaster #emotion:neutral
Це ж не адреса… #speaker:lilia #emotion:confused
Тут інших не буває. Не барися, листоноше. #speaker:postmaster #emotion:smile #give_item:item_letter_fisher
-> marka_intro

=== marka_intro ===
Не дивись так. Я журавлик, а не голуб. #speaker:marka
Колись я був листом. Мене не доставили. Тож тепер я доставляю тебе. #speaker:marka
+ [Чому мій рукав мокрий?]
    Чому мій рукав мокрий? #speaker:lilia
    Кажуть, у Межі кожен щось приносить із собою. Ти принесла воду. #speaker:marka
+ [Ходімо.]
    Ходімо. #speaker:lilia
- -> finish

=== postmaster_letter1 ===
// Площа Пошти (головна сцена): Ліля підходить, коротко говорить і отримує перший лист.
// Довге знайомство — у вступі (postmaster_intro, сцена Sandbox_Intro), тут його не повторюємо.
// Після пролога (Пантелеймон пішов) — другий лист і арка на Вулицю Ниток.
{ prologue_done:
    -> postmaster_letter2
}
{ postmaster_letter1 > 1:
    Лист сам себе не доставить, дитино. #speaker:postmaster #emotion:neutral
    -> finish
}
А, листоноша. Виспалась? #speaker:postmaster #emotion:smile
+ [Готова працювати.]
    Готова працювати. #speaker:lilia #emotion:neutral
    От і славно. #speaker:postmaster #emotion:smile
+ [У мене ж немає вибору, так?]
    У мене ж немає вибору, так? #speaker:lilia #emotion:sad
    Вибір є завжди. Просто не всі його помічають. #speaker:postmaster #emotion:neutral
- Тримай. «Тому, хто ловить там, де немає води». #speaker:postmaster #emotion:neutral #give_item:item_letter_fisher
Запечатаним. Пам'ятаєш? #speaker:postmaster #emotion:angry
Пам'ятаю. #speaker:lilia #emotion:scared
-> finish

=== postmaster_talk ===
// повторна розмова з Поштмейстером, коли завдання ще не виконано
Лист сам себе не доставить, дитино. #speaker:postmaster
-> finish

=== marka_kettle ===
// Ліля підходить до чайника (SteamKettle, 1-А, x = 6)
Над парою конверт відкривається сам. Але ти ж не будеш, правда? #speaker:marka
-> finish

=== marka_after_reading ===
// після читання будь-якого листа над парою
Ну от. Тепер ти знаєш те, чого не мала знати. #speaker:marka
-> finish

=== marka_hello ===
// клік по Марці (Companion_Marka)
Я Марка, твій супутник! #speaker:marka
-> finish

=== not_for_me ===
// лист віддали не тому адресату
Це не мені. #speaker:addressee
Адреса не та. Подивись уважніше, листоноше. #speaker:marka
-> finish

=== panteleimon ===
// розмова з рибалкою (1-А, x = 28)
{ lantern_lit:
    Світло… Давно тут не було світла. #speaker:panteleimon
- else:
    Темно, дитино. Очі вже не ті, не розгледжу ні букви. #speaker:panteleimon
}
-> finish

=== panteleimon_letter ===
// Ліля віддає лист 1 (LetterManager.Deliver)
{ not lantern_lit:
    Темно, дитино. Очі вже не ті, не розгледжу ні букви. #speaker:panteleimon
    Йому бракує світла. Може, ліхтар біля мосту? #speaker:marka
    -> finish
}
(Пантелеймон підносить лист до ліхтаря і довго читає.) #speaker:narrator
{ read_letter_fisher:
    Печатка зламана… Та що вже. Тут ніяких таємниць — лише літо на річці. #speaker:panteleimon
}
Олечка… Пам'ятає. #speaker:panteleimon
Ну то й мені вже не треба тут сидіти. #speaker:panteleimon
~ prologue_done = true
Бачила? Прочитав — і відпустив. Так це працює. #speaker:marka
-> finish

=== postmaster_letter2 ===
// Ліля повертається на Пошту після пролога: другий лист і відчинена арка
{ postmaster_letter2 > 1:
    Вулиця Ниток — за аркою. Не барися. #speaker:postmaster
    -> finish
}
{ read_letter_fisher:
    Цікаві листи пишуть живі, правда? #speaker:postmaster
}
Наступний. «Тій, що шиє для тієї, хто не прийде». Вулиця Ниток — за аркою. #speaker:postmaster #give_item:item_letter_tailor
-> finish


// =====================================================================
// ГЛАВА 1 — Вулиця Ниток (L1_TailorStreet), Дім Кравчині (L1_TailorHouse)
// =====================================================================

=== umbrella_woman ===
// Жінка з парасолькою (1-Б, x = 8), підказка 1
Та, що шиє? Шукай вікно, де світло ніколи не гасне. #speaker:umbrella_woman
-> finish

=== clockmaker ===
// Годинникар (1-Б, x = 24), підказка 2
Вона обплутала дім нитками, щоб донька не пройшла повз. Сорок років ниток. #speaker:clockmaker
-> finish

=== marka_scissors ===
// Ножиці використали на нитках
Різати чужу роботу? Вона сорок років це плела. #speaker:marka
-> finish

=== stefania ===
// пані Стефанія (1-В, x = 11)
{ tailor_left:
    -> finish
}
{ tailor_closed:
    (Двері задньої кімнати зачинені. За ними хтось тихо плаче.) #speaker:narrator
    -> finish
}
Обережно, не наступи на мереживо. Сукня майже готова. Марійка от-от прийде. #speaker:stefania
+ [Віддати лист: «Вам лист, пані Стефаніє».]
    -> path_a
+ {read_letter_tailor} [Сказати: «Ваша донька шкодує, що втекла».]
    -> path_b
+ [Поки нічого не казати.]
    -> finish

= path_a
// ШЛЯХ А: не читала або прочитала, але не скористалася знанням
Вам лист, пані Стефаніє. Від Марії. #speaker:lilia
(Стефанія читає. Голка випадає з її пальців.) #speaker:narrator
Сорок років я шила не ту сукню… #speaker:stefania
Візьми клубок, дитино. Мені він більше не потрібен. #speaker:stefania #give_item:item_red_thread
~ tailor_left = true
-> finish

= path_b
// ШЛЯХ Б: прочитала і скористалася знанням
Ваша донька шкодує, що втекла. #speaker:lilia
Звідки ти знаєш?! Хто дав тобі право читати?! #speaker:stefania #sfx:sfx_door_slam
~ tailor_closed = true
Гарно вийшло. Тепер доведеться шукати інший підхід. #speaker:marka
-> finish

=== stefania_button ===
// ШЛЯХ Б: Ліля підсуває Ґудзик під двері задньої кімнати (1-В, x = 15)
{ not tailor_closed:
    Ґудзик із сукні? Спершу треба, щоб було кому його підсунути. #speaker:marka
    -> finish
}
(Тиша. Потім клацає замок.) #speaker:narrator
Це… з її сукні. Вона таки зачепилася… #speaker:stefania
Давай лист. #speaker:stefania
(Стефанія читає лист, притискаючи ґудзик до грудей.) #speaker:narrator
Візьми наперсток. Хай береже тобі пальці. #speaker:stefania #give_item:item_thimble
~ tailor_closed = false
~ tailor_left = true
-> finish

=== demo_end_marka ===
// репліка Марки на екрані «Межа пам'ятає» (DemoEnd)
Далі буде важче. Там, у сортувальні, хтось стукає. #speaker:marka
-> finish


// =====================================================================
// ПІДКАЗКИ МАРКИ (HintSystem): ціль → рівні .l1 / .l2 / .l3
// Unity викликає, наприклад, StartKnot("hint_lantern.l2")
// Рівень 3 на пазлах ниток: Unity ще й підсвічує одну неправильну плитку
// =====================================================================

=== hint_fisher_address ===
= l1
Хто ловить там, де немає води? Рибалки ж ловлять у річці… #speaker:marka
{ test_mode: -> l2 }
-> finish
= l2
Річка давно висохла, а хтось усе ще сидить із вудкою. Біля мосту, мабуть. #speaker:marka
{ test_mode: -> l3 }
-> finish
= l3
Старий з вудкою біля мосту, в кінці площі. Віддай лист йому. #speaker:marka
-> finish

=== hint_lantern ===
= l1
Він каже, що темно. Може, тут комусь бракує світла? #speaker:marka
{ test_mode: -> l2 }
-> finish
= l2
Біля Пошти лежить свічка, а далі на площі — сірники. Зазирни в сумку. #speaker:marka
{ test_mode: -> l3 }
-> finish
= l3
Поєднай у сумці Свічку і Сірники, а потім запали ліхтар біля мосту. #speaker:marka
-> finish

=== hint_tailor_address ===
= l1
Хто шиє для того, хто не прийде? Хтось дуже впертий. #speaker:marka
{ test_mode: -> l2 }
-> finish
= l2
Спитай мешканців на Вулиці Ниток. #speaker:marka
{ test_mode: -> l3 }
-> finish
= l3
Будинок у кінці вулиці, де вікно світиться. #speaker:marka
-> finish

=== hint_gate ===
// пазл «Хвіртка» 3×3 (1-Б, x = 38)
= l1
Нитка має дійти від голки до котушки й ніде не обірватися. #speaker:marka
{ test_mode: -> l2 }
-> finish
= l2
Почни від голки. Повертай плитку, доки нитка не засвітиться далі. #speaker:marka
{ test_mode: -> l3 }
-> finish
= l3
Ось ця плитка дивиться не туди. Поверни її. #speaker:marka
-> finish

=== hint_hall ===
// пазл «Коридор» 4×4 (1-В, x = 4)
= l1
Тут дві нитки — червона й біла. Кожна шукає свою котушку. #speaker:marka
{ test_mode: -> l2 }
-> finish
= l2
Хрест пропускає дві нитки й не з'єднує їх. Постав його там, де вони перетинаються. #speaker:marka
{ test_mode: -> l3 }
-> finish
= l3
Ось ця плитка повернута не так. #speaker:marka
-> finish

=== hint_closed_door ===
// шлях Б: Стефанія зачинилася
= l1
Вона не хоче говорити з тією, що читала її лист. Потрібне щось, що скаже за тебе. #speaker:marka
{ test_mode: -> l2 }
-> finish
= l2
На Вулиці Ниток під бруківкою щось блищало. Перламутрове. #speaker:marka
{ test_mode: -> l3 }
-> finish
= l3
Ґудзик біля хвіртки на Вулиці Ниток. Підсунь його під двері задньої кімнати. #speaker:marka
-> finish


// =====================================================================
// СЛУЖБОВЕ
// =====================================================================

=== finish ===
{ test_mode: -> test_hub }
-> END


// =====================================================================
// ТЕСТ — лише для Inky. Варіанти в [квадратних дужках] імітують дії
// гравця в Unity. Пройди обидва шляхи: А (віддати лист) і Б (прочитати
// лист 2 → сказати Стефанії → підсунути ґудзик).
// =====================================================================

=== test_start ===
~ test_mode = true
ТЕСТ ПРОЛОГА І ГЛАВИ 1.
-> test_hub

=== test_hub ===
Стан → пролог: {prologue_done:пройдено|ні}; ліхтар: {lantern_lit:горить|ні}; прочитано рибалці: {read_letter_fisher:так|ні}; прочитано кравчині: {read_letter_tailor:так|ні}; Стефанія: {tailor_left:пішла|{tailor_closed:зачинилася|чекає}}; Втручання: {intervention}
+ {not prologue_done} [Розмова: Поштмейстер (початок гри)]
    -> postmaster_intro
+ {not prologue_done} [Дія: підійти до чайника]
    -> marka_kettle
+ {not prologue_done && not read_letter_fisher} [Дія: прочитати лист 1 над парою]
    ~ read_letter_fisher = true
    ~ intervention += 1
    -> marka_after_reading
+ {not prologue_done} [Розмова: Пантелеймон]
    -> panteleimon
+ {not prologue_done && not lantern_lit} [Дія: запалити ліхтар свічкою]
    ~ lantern_lit = true
    -> test_hub
+ {not prologue_done} [Дія: віддати лист 1 Пантелеймону]
    -> panteleimon_letter
+ {not prologue_done} [Підказки: адреса рибалки (усі рівні)]
    -> hint_fisher_address.l1
+ {not prologue_done} [Підказки: ліхтар (усі рівні)]
    -> hint_lantern.l1
+ {prologue_done && not tailor_left} [Розмова: Поштмейстер (лист 2)]
    -> postmaster_letter2
+ {prologue_done && not read_letter_tailor && not tailor_left} [Дія: прочитати лист 2 над парою]
    ~ read_letter_tailor = true
    ~ intervention += 1
    -> marka_after_reading
+ {prologue_done && not tailor_left} [Розмова: Жінка з парасолькою]
    -> umbrella_woman
+ {prologue_done && not tailor_left} [Розмова: Годинникар]
    -> clockmaker
+ {prologue_done && not tailor_left} [Дія: ножиці на нитки]
    -> marka_scissors
+ {prologue_done && not tailor_left} [Дія: віддати лист не тому адресату]
    -> not_for_me
+ {prologue_done && not tailor_left} [Підказки: адреса кравчині (усі рівні)]
    -> hint_tailor_address.l1
+ {prologue_done && not tailor_left} [Підказки: пазл «Хвіртка» (усі рівні)]
    -> hint_gate.l1
+ {prologue_done && not tailor_left} [Підказки: пазл «Коридор» (усі рівні)]
    -> hint_hall.l1
+ {prologue_done && not tailor_left} [Розмова: пані Стефанія]
    -> stefania
+ {tailor_closed} [Підказки: зачинені двері (усі рівні)]
    -> hint_closed_door.l1
+ {tailor_closed} [Дія: підсунути ґудзик під двері]
    -> stefania_button
+ {tailor_left} [Кінець глави 1 → «Межа пам'ятає»]
    Главу 1 завершено. Пані Стефанія перейшла міст.
    ~ test_mode = false
    -> demo_end_marka
+ [Скинути все й почати тест спочатку]
    ~ prologue_done = false
    ~ read_letter_fisher = false
    ~ read_letter_tailor = false
    ~ tailor_closed = false
    ~ tailor_left = false
    ~ lantern_lit = false
    ~ intervention = 0
    -> test_hub
