using System;
using System.Collections.Generic;
using System.Text;

namespace BethKEY
{
    /// Plain-English descriptions shown when hovering over an action.
    /// Lookup order: game + section + action, section + action, game + action (main), action in any section, whole section.
    public static class ActionHelp
    {
        static readonly HashSet<string> MainCats = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Movement", "Combat", "Actions", "Menus", "Hotkeys", "System", "Main Gameplay", "Other" };

        static readonly Dictionary<string, string> D = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        static string N(string s)
        {
            StringBuilder b = new StringBuilder();
            foreach (char c in s ?? "") if (char.IsLetterOrDigit(c)) b.Append(char.ToLowerInvariant(c));
            return b.ToString();
        }

        static void A(string key, string text)
        {
            // key: "[game:]section|action"   section "main" = normal gameplay, "*" = any section, action "*" = whole section
            string game = "", rest = key;
            int c = key.IndexOf(':');
            if (c > 0) { game = key.Substring(0, c) + ":"; rest = key.Substring(c + 1); }
            string[] p = rest.Split('|');
            foreach (string ev in p[1].Split(','))
                D[game + (p[0] == "*" || p[0] == "main" ? p[0] : N(p[0])) + "|" + (ev == "*" ? "*" : N(ev))] = text;
        }

        public static string Get(GameDef g, BindRow r)
        {
            string game = g == null ? "" : g.ProcessName + ":";
            string ctx = MainCats.Contains(r.Context ?? "") ? "main" : N(r.Context);
            string ev = N(r.Event);
            string t;
            foreach (string k in new[] { game + ctx + "|" + ev, ctx + "|" + ev, game + "*|" + ev, "*|" + ev, game + ctx + "|*", ctx + "|*" })
                if (D.TryGetValue(k, out t)) return t;
            if (r.Label != null && r.Label.EndsWith("(menu key)"))
                return "Команда меню, назначаемая игрой на кнопку геймпада. На ПК используйте клавишу, показанную рядом с подсказкой на экране.";
            return null;
        }

        static ActionHelp()
        {
            const string Dev = "Остаточный отладочный ключ. В выпущенной игре обычно ничего не делает.";

            // ---- Movement ----
            A("main|Forward", "Идти или бежать вперёд.");
            A("main|Back", "Идти или бежать назад.");
            A("main|StrafeLeft,Slide Left", "Шаг вбок влево без поворота.");
            A("main|StrafeRight,Slide Right", "Шаг вбок вправо без поворота.");
            A("main|Run", "Удерживайте, чтобы переключаться между бегом и ходьбой (наоборот параметру «Всегда бегать»).");
            A("main|ToggleAlwaysRun,Always Run", "Переключение между постоянным бегом и постоянной ходьбой.");
            A("main|AutoMove,Auto Move", "Двигаться вперёд без удержания клавиши. Нажмите ещё раз или «Вперёд»/«Назад», чтобы остановиться.");
            A("main|Jump", "Прыжок.");
            A("main|Sprint", "Удерживайте для ускоренного бега.");
            A("Fallout4:main|Sprint", "Удерживайте для ускоренного бега. Расходует очки действий.");
            A("SkyrimSE:main|Sprint", "Удерживайте для ускоренного бега. Расходует запас сил (верхом: галоп).");
            A("main|Sneak,Crouch/Sneak", "Включение скрытности (присесть).");

            // ---- Combat ----
            A("main|PrimaryAttack,Use", "Выстрелить или ударить снаряжённым оружием. Удерживайте для непрерывной стрельбы из автоматического оружия.");
            A("main|SecondaryAttack,Block", "Удерживайте для прицеливания через мушку или оптику либо для блока в ближнем бою.");
            A("main|Melee", "Удар оружием. Удерживайте, чтобы бросить снаряжённую гранату или мину.");
            A("main|ReadyWeapon,Ready Item", "Короткое нажатие — перезарядка. Удерживайте, чтобы убрать или достать оружие.");
            A("SkyrimSE:main|Ready Weapon", "Достать или убрать оружие и заклинания.");
            A("main|Ammo Swap", "Переключить снаряжённое оружие на следующий тип боеприпасов.");
            A("main|VATS", "Войти в режим прицеливания V.A.T.S.");
            A("main|Left Attack/Block", "Атака или заклинание левой рукой. С двуручным оружием или когда оружие только в правой руке — удерживайте для блока.");
            A("main|Right Attack/Block", "Атака или заклинание правой рукой. Удерживайте для сильной атаки.");
            A("main|Shout", "Использовать снаряжённый крик или способность. Удерживайте для более сильного крика.");

            // ---- Actions ----
            A("main|Activate", "Взаимодействовать с тем, на что смотрите: поговорить, открыть, подобрать, использовать, сесть.");
            A("main|Grab", "Удерживайте, чтобы схватить и нести физический объект или тело; отпустите, чтобы бросить.");
            A("main|TogglePOV,Toggle POV", "Переключение между видом от первого и от третьего лица.");
            A("main|ZoomIn", "Приблизить камеру к персонажу. Максимальное приближение включает вид от первого лица.");
            A("main|ZoomOut", "Отдалить камеру от персонажа. Из вида от первого лица переключает на вид от третьего лица.");
            A("main|Wait", "Открыть меню ожидания, чтобы пропустить время.");
            A("main|Rest", "Открыть меню ожидания, чтобы пропустить время (в INI — \"Rest\").");

            // ---- Menus ----
            A("main|Pipboy,Menu Mode", "Открыть или закрыть Пип-Бой.");
            A("main|Quickkeys", "Открыть колесо избранного, чтобы использовать или назначить избранное.");
            A("main|Quicksave", "Мгновенно сохранить игру в слот быстрого сохранения.");
            A("main|Quickload", "Загрузить последнее быстрое сохранение.");
            A("main|Pause", "Открыть меню паузы (сохранение, загрузка, настройки, выход).");
            A("main|QuickStats", "Открыть Пип-Бой сразу на вкладке СТАТ.");
            A("main|QuickInventory", "Открыть Пип-Бой сразу на вкладке ИНВ.");
            A("main|QuickData", "Открыть Пип-Бой сразу на вкладке ДАННЫЕ (квесты, мастерские).");
            A("main|QuickMap", "Открыть Пип-Бой сразу на вкладке КАРТА.");
            A("main|QuickRadio", "Открыть Пип-Бой сразу на вкладке РАДИО.");
            A("SkyrimSE:main|Quick Inventory", "Открыть инвентарь напрямую.");
            A("SkyrimSE:main|Quick Magic", "Открыть меню «Магия» напрямую.");
            A("SkyrimSE:main|Quick Stats", "Открыть меню навыков (созвездий) напрямую.");
            A("SkyrimSE:main|Quick Map", "Открыть карту мира напрямую.");
            A("main|Tween Menu", "Открыть меню, ведущее к разделам «Предметы», «Магия», «Карта» и «Навыки».");
            A("main|Favorites", "Открыть меню «Избранное», чтобы быстро экипировать избранные предметы и заклинания.");
            A("main|Journal", "Открыть журнал (квесты, статистика, сохранение и настройки).");

            // ---- Hotkeys ----
            for (int i = 1; i <= 12; i++)
                A("main|Quickkey" + i, "Использовать предмет из ячейки " + i + " колеса избранного (назначается в Пип-Бое).");
            for (int i = 1; i <= 8; i++)
            {
                A("SkyrimSE:main|Hotkey" + i, "Экипировать избранное, назначенное на горячую клавишу " + i + " (в меню «Избранное» подсветите предмет или заклинание и нажмите эту клавишу).");
                A("FalloutNV:main|Hotkey" + i, "Использовать или экипировать предмет, назначенный на горячую клавишу " + i + " (назначается в Пип-Бое).");
                A("Fallout3:main|Hotkey" + i, "Использовать или экипировать предмет, назначенный на горячую клавишу " + i + " (назначается в Пип-Бое).");
            }

            // ---- System ----
            A("main|Screenshot", "Сохранить снимок экрана.");
            A("main|Multi-Screenshot", Dev);
            A("main|Console", "Открыть или закрыть консоль разработчика (по умолчанию ~).");
            A("main|ConsoleToggles", Dev);
            A("main|CameraPath", Dev);

            // ---- Menu navigation ----
            A("Menu Navigation|Accept", "Подтвердить или выбрать подсвеченный вариант в меню.");
            A("Menu Navigation|Cancel", "Вернуться назад или закрыть текущее меню.");
            A("Menu Navigation|Up,Down,Left,Right", "Перемещать подсветку в меню.");
            A("*|Console", "Открыть консоль, пока это меню открыто.");
            A("Menu Buttons|LShoulder", "Переключиться на предыдущую вкладку или категорию в меню.");
            A("Menu Buttons|RShoulder", "Переключиться на следующую вкладку или категорию в меню.");
            A("Cursor|Click", "Щелчок меню-курсором.");
            A("Left Stick Cursor|Click", "Щелчок меню-курсором, когда им управляет стик (карта Пип-Боя, дерево перков).");

            // ---- Console ----
            A("Console|PickPrevious", "При открытой консоли выбрать предыдущий объект под перекрестьем.");
            A("Console|PickNext", "При открытой консоли выбрать следующий объект под перекрестьем.");
            A("Console|Up", "Показать предыдущую введённую команду.");
            A("Console|Down", "Показать следующую введённую команду.");
            A("Console|PageUp", "Прокрутить вывод консоли вверх.");
            A("Console|PageDown", "Прокрутить вывод консоли вниз.");
            A("Console|NextFocus", "Перевести фокус на следующую панель консоли.");
            A("Console|PreviousFocus", "Перевести фокус на предыдущую панель консоли.");
            A("Console|Console", "Закрыть консоль.");

            // ---- Debug ----
            A("Debug Text|PrevPage", "Предыдущая страница отладочного текста на экране (консольная команда tdt).");
            A("Debug Text|NextPage", "Следующая страница отладочного текста на экране (консольная команда tdt).");
            A("Debug Text|PrevSubPage", "Предыдущая подстраница отладочного текста на экране.");
            A("Debug Text|NextSubPage", "Следующая подстраница отладочного текста на экране.");
            A("Debug Overlay|*", "Управление отладочной заставкой разработчика. Обычно не используется в выпущенной игре.");
            A("Debug Map|ZoomIn", "Приблизить отладочную карту разработчика.");
            A("Debug Map|ZoomOut", "Отдалить отладочную карту разработчика.");

            // ---- Books ----
            A("Books|PrevPage", "Перевернуть страницу книги, заметки или записи терминала назад.");
            A("Books|NextPage", "Перевернуть страницу книги, заметки или записи терминала вперёд.");

            // ---- Free camera ----
            A("Free Camera (TFC)|CameraZUp", "Свободная камера (консольная команда tfc): движение вверх относительно направления взгляда камеры.");
            A("Free Camera (TFC)|CameraZDown", "Свободная камера (консольная команда tfc): движение вниз относительно направления взгляда камеры.");
            A("Free Camera (TFC)|WorldZUp", "Свободная камера (консольная команда tfc): движение строго вверх.");
            A("Free Camera (TFC)|WorldZDown", "Свободная камера (консольная команда tfc): движение строго вниз.");

            // ---- Lockpicking ----
            A("Lockpicking|RotateLock", "Повернуть замок, пытаясь его открыть.");
            A("Lockpicking|DebugMode", Dev);
            A("Lockpicking|Cancel", "Перестать взламывать замок.");

            // ---- V.A.T.S. ----
            A("V.A.T.S.|Pick", "Назначить выстрел по подсвеченной части тела.");
            A("V.A.T.S.|Accept", "Выполнить назначенные выстрелы.");
            A("V.A.T.S.|Cancel", "Отменить назначенный выстрел или покинуть V.A.T.S.");
            A("V.A.T.S.|NextTarget", "Переключиться на следующую цель.");
            A("V.A.T.S.|PrevTarget", "Переключиться на предыдущую цель.");
            A("V.A.T.S.|NextPart", "Подсветить следующую часть тела.");
            A("V.A.T.S.|PrevPart", "Подсветить предыдущую часть тела.");
            A("V.A.T.S.|ZoomIn", "Приблизить вид V.A.T.S. к цели.");
            A("V.A.T.S. Playback|Cancel", "Пропустить камеру V.A.T.S.");
            A("V.A.T.S. Playback|ApplyCritical", "Использовать критический удар, когда шкала критов заполнена.");

            // ---- Prompts ----
            A("Multi-Activate Prompt|*", "Выбрать один из вариантов подсказки с несколькими действиями (например, «Передать», «Взять всё», «Поговорить»). Подсказка показывает, какая клавиша что делает.");

            // ---- Workshop ----
            A("Workshop|Accept", "Разместить, построить или выбрать в режиме Мастерской.");
            A("Workshop|Cancel", "Вернуться назад или отложить удерживаемый предмет в режиме Мастерской.");
            A("Workshop|Up,Down,Left,Right", "Перемещаться по меню строительства Мастерской.");
            A("Workshop|CloseMenu", "Выйти из режима Мастерской.");
            A("Workshop|AlternateControlsKey", "Удерживайте, чтобы использовать альтернативное управление Мастерской (показывается на экране, пока удерживается).");
            A("Workshop|Jump", "Прыжок в режиме Мастерской.");
            A("Workshop|*", "Команда Мастерской, назначаемая игрой на кнопку геймпада. Подсказки на экране показывают, что она делает.");
            A("Workshop (Extra)|*", "Команда Мастерской, назначаемая игрой на кнопку геймпада. Подсказки на экране показывают, что она делает.");

            // ---- Misc FO4 ----
            A("Scope|Steady", "Задержите дыхание, чтобы стабилизировать оружие с оптикой.");
            A("Sit / Wait|StartWait", "Сидя, открыть меню ожидания, чтобы пропустить время.");
            A("Looks Menu|Done", "Завершить редактирование внешности.");
            A("Looks Menu|Cancel", "Вернуться назад в меню внешности.");
            A("Looks Menu|Accept", "Выбрать подсвеченный вариант в меню внешности.");
            A("Looks Menu|KeyLeft,KeyRight,KeyUp,KeyDown", "Настроить выбранный элемент внешности в этом меню.");
            A("Looks Menu|*", "Команда меню внешности (создание персонажа). Подсказки на экране показывают, что она делает.");
            A("Pause Menu|DeleteSave", "Удалить подсвеченное сохранение.");
            A("Pause Menu|ResetToDefault", "Сбросить страницу настроек к значениям по умолчанию.");
            A("Pause Menu|Accept", "Выбрать в меню паузы.");
            A("Pause Menu|Cancel", "Вернуться назад или закрыть меню паузы.");
            A("Level Up Menu|ZoomIn", "Приблизить дерево перков.");
            A("Level Up Menu|ZoomOut", "Отдалить дерево перков.");
            A("Level Up Menu Paging|PrevPerk", "Показать предыдущий ранг подсвеченного перка.");
            A("Level Up Menu Paging|NextPerk", "Показать следующий ранг подсвеченного перка.");
            A("Main Menu|DeleteSave", "Удалить подсвеченное сохранение.");
            A("Main Menu|Start", "Продолжить после титульного экрана («Press start»).");
            A("Transfer Menu (Quick Container)|QuickContainerUp", "Прокрутить список вверх в окне быстрого подбора при взгляде на контейнер.");
            A("Transfer Menu (Quick Container)|QuickContainerDown", "Прокрутить список вниз в окне быстрого подбора при взгляде на контейнер.");
            A("Vertibird|Land", "Приказать «Вертибёрд» сесть.");
            A("Vertibird|BailOut", "Выпрыгнуть из «Вертибёрда».");
            A("Video Playback|Start", "Пропустить видео.");
            A("Robot Workbench|RotateLeft", "Повернуть робота влево на верстаке робототехники.");
            A("Robot Workbench|RotateRight", "Повернуть робота вправо на верстаке робототехники.");

            // ---- Creations ----
            A("Creations Menu|SearchEdit", "Ввести текст в поле поиска раздела «Творения».");
            A("Creations Menu|LoadOrderAndDelete", "Открыть порядок загрузки или удалить выбранное творение.");
            A("Creations Menu|LoadOrderArchive", "Заархивировать выбранное творение в порядке загрузки.");
            A("Creations Menu|CategorySideBar", "Показать список категорий.");
            A("Creations Menu|Options", "Показать параметры выбранного творения.");
            A("Creations Menu|PurchaseCredits", "Открыть страницу покупки кредитов «Творений».");
            A("Creations Menu|Filter", "Отфильтровать список творений.");
            A("Creations Menu|LikeUnlike", "Поставить или убрать отметку «нравится» у выбранного творения.");
            A("Creations Menu|MouseScrollUp", "Прокрутить список творений вверх.");
            A("Creations Menu|MouseScrollDown", "Прокрутить список творений вниз.");
            A("Creations Menu|ShowMissingContent", "Показать творения, нужные вашим сохранениям, но не установленные.");
            A("Creations Menu|Accept,MouseClickAccept", "Выбрать подсвеченное творение или кнопку.");
            A("Creations Menu|Cancel", "Вернуться назад или закрыть меню «Творения».");
            A("Creations Menu|Up,Down,Left,Right", "Перемещать подсветку в меню «Творения».");
            A("Creations Menu|HomeKey", "Перейти к началу списка или текста.");
            A("Creations Menu|EndKey", "Перейти к концу списка или текста.");
            A("Creations Menu|DeleteKey", "Удалить текст в поле поиска.");
            A("Creations Menu|*", "Команда меню «Творения».");

            // ---- Skyrim menus ----
            A("Item Menus|Item Zoom", "Приблизить трёхмерный вид подсвеченного предмета.");
            A("Map Menu|ZoomIn", "Приблизить карту.");
            A("Map Menu|ZoomOut", "Отдалить карту.");
            A("Map Menu|MapLookMode", "Удерживайте, чтобы вращать и наклонять карту.");
            A("Map Menu|PlacePlayerMarker", "Поставить или убрать свой маркер на карте.");
            A("Map Menu|PlayerPosition", "Центрировать карту на вашем положении.");
            A("Map Menu|LocalMap", "Переключение между картой мира и локальной картой.");
            A("Map Menu|LocalMapMoveMode", "Удерживайте, чтобы перемещать локальную карту.");
            A("Map Menu|Journal", "Открыть журнал прямо с карты.");
            A("Journal|ZoomIn,ZoomOut", "Прокрутка колесом мыши в журнале.");
        }
    }
}
