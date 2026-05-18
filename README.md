# Лабораторная работа: пользовательские контролы, DependencyProperty, RoutedEvent, RoutedUICommand

Этот файл описывает, как в проекте реализованы пункты задания:

1. Два пользовательских элемента управления  
2. DependencyProperty с `ValidateValueCallback` и `CoerceValueCallback`  
3. RoutedEvent с типами маршрутизации `Direct`, `Tunnel`, `Bubble` и демонстрация различий  
4. Использование контролов в проекте  
5. Пользовательская команда на базе `RoutedUICommand`

---

## 1 Два пользовательских элемента управления

### 1.1 `DiscountBarControl`
- UI: `lab4-5/Controls/DiscountBarControl.xaml`
- Логика: `lab4-5/Controls/DiscountBarControl.xaml.cs`

Назначение:
- редактирование скидки через `Slider`;
- визуальный индикатор процента скидки (заливка полосы);
- вывод текущего значения в процентах;
- вывод `Direct`-события внутри контрола (`DirectText`).

### 1.2 `RatingStarsPicker`
- UI: `lab4-5/Controls/RatingStarsPicker.xaml`
- Логика: `lab4-5/Controls/RatingStarsPicker.xaml.cs`

Назначение:
- выбор рейтинга кнопками-звездами;
- перекраска звезд в зависимости от текущего рейтинга;
- вывод `Direct`-события внутри контрола (`DirectText`).

---

## 2 DependencyProperty + Validate + Coerce

## 2.1 Реализация в `DiscountBarControl`
Файл: `lab4-5/Controls/DiscountBarControl.xaml.cs`

### `DiscountPercentProperty`
- зарегистрирован через `DependencyProperty.Register(...)`;
- `ValidateValueCallback`: `ValidateDiscount`;
- `CoerceValueCallback`: `CoerceDiscount`.

Что делает:
- валидация: значение должно быть `decimal`;
- коррекция: диапазон `0..100`, приведение к шагу `SnapStep`, округление до 2 знаков.

### `SnapStepProperty`
- `ValidateValueCallback`: `ValidateSnapStep`;
- `CoerceValueCallback`: `CoerceSnapStep`.

Что делает:
- валидация: `decimal`, больше 0, не больше 25;
- коррекция: ограничение в пределах `0.5..25`;
- при изменении шага вызывается `CoerceValue(DiscountPercentProperty)`.

### `BarHeightProperty`
- `ValidateValueCallback`: `ValidateBarHeight`;
- `CoerceValueCallback`: `CoerceBarHeight`.

Что делает:
- валидация: не `NaN`, не `Infinity`;
- коррекция: ограничение высоты в пределах `8..40`.

### Где используются свойства `DiscountBarControl`
- Объявление DP и CLR-оберток: `lab4-5/Controls/DiscountBarControl.xaml.cs`
- Использование `BarHeight` в UI контрола: `lab4-5/Controls/DiscountBarControl.xaml` (`Border.Height` через `ElementName=Root`)
- Установка значений снаружи: `lab4-5/MainWindow.xaml`
  - `DiscountPercent="{Binding DiscountPercent, ...}"`
  - `SnapStep="1"`
  - `BarHeight="16"`
- Источник данных для `DiscountPercent`: `lab4-5/Models/GuitarProduct.cs` (`DiscountPercent`)
- Дополнительное влияние в модели:
  - `PriceWithDiscount` пересчитывается от `DiscountPercent` в `lab4-5/Models/GuitarProduct.cs`
  - начальные значения товаров (`DiscountPercent`) задаются в `lab4-5/Services/ProductRepository.cs`

## 2.2 Реализация в `RatingStarsPicker`
Файл: `lab4-5/Controls/RatingStarsPicker.xaml.cs`

### `RatingProperty`
- `ValidateValueCallback`: `ValidateRating`;
- `CoerceValueCallback`: `CoerceRating`.

Что делает:
- валидация: `double`, не `NaN`, не `Infinity`;
- коррекция: ограничение `0..MaxRating`;
- округление до шага `0.5` (`Math.Round(v * 2, MidpointRounding.AwayFromZero) / 2`).

### `MaxRatingProperty`
- `ValidateValueCallback`: `ValidateMaxRating`;
- `CoerceValueCallback`: `CoerceMaxRating`.

Что делает:
- валидация: `double > 0`, не `NaN`, не `Infinity`;
- коррекция: ограничение `1..10`;
- при изменении вызывает `CoerceValue(RatingProperty)`.

Примечание: текущий UI выбора звездами дает в основном целые значения (1..5), но `CoerceRating` позволяет корректно обработать дробные значения, если они придут из биндинга/кода.

### Где используются свойства `RatingStarsPicker`
- Объявление DP и CLR-оберток: `lab4-5/Controls/RatingStarsPicker.xaml.cs`
- Установка значений снаружи: `lab4-5/MainWindow.xaml`
  - `Rating="{Binding Rating, ...}"`
  - `MaxRating="5"`
- Источник данных для `Rating`: `lab4-5/Models/GuitarProduct.cs`
- Дополнительные места, где участвует `Rating`:
  - окно добавления товара (`lab4-5/Views/AddProductWindow.xaml`, поле `TbRating`)
  - начальные значения товаров (`Rating`) в `lab4-5/Services/ProductRepository.cs`

---

## 3) RoutedEvent (Direct / Tunnel / Bubble) и демонстрация различий

## 3.1 Где объявлены события

### В `DiscountBarControl`
Файл: `lab4-5/Controls/DiscountBarControl.xaml.cs`
- `PreviewDiscountChangingEvent` -> `RoutingStrategy.Tunnel`
- `DiscountChangingEvent` -> `RoutingStrategy.Bubble`
- `DiscountChangedDirectEvent` -> `RoutingStrategy.Direct`

### В `RatingStarsPicker`
Файл: `lab4-5/Controls/RatingStarsPicker.xaml.cs`
- `PreviewRatingChangingEvent` -> `RoutingStrategy.Tunnel`
- `RatingChangingEvent` -> `RoutingStrategy.Bubble`
- `RatingChangedDirectEvent` -> `RoutingStrategy.Direct`

## 3.2 Где события поднимаются (`RaiseEvent`)
- `DiscountBarControl`: в `OnSliderValueChanged`
- `RatingStarsPicker`: в `OnStarClick`

Порядок вызова одинаковый:
1. `Preview...` (Tunnel)
2. `...Changing` (Bubble)
3. `...Direct` (Direct)

## 3.2.1 Жизненный цикл события в `DiscountBarControl`
Файл: `lab4-5/Controls/DiscountBarControl.xaml.cs`, метод `OnSliderValueChanged`

Последовательность:
1. Берется старое значение `oldVal = DiscountPercent`.
2. Из `Slider` берется новое "сырое" значение `raw`.
3. Поднимается `PreviewDiscountChangingEvent` (Tunnel) с `oldVal/raw`.
4. Если `Handled=true`, изменение отменяется.
5. Присваивается `DiscountPercent = raw` (на этом шаге работает `CoerceDiscount`).
6. Поднимается `DiscountChangingEvent` (Bubble) уже с итоговым значением.
7. Поднимается `DiscountChangedDirectEvent` (Direct).

Почему в логе бывает `60 -> 60.1` на Tunnel и `60 -> 60` на Bubble:
- Tunnel логирует `raw` до coercion;
- Bubble логирует значение после `CoerceDiscount` (при `SnapStep=1` это целое).

## 3.2.2 Жизненный цикл события в `RatingStarsPicker`
Файл: `lab4-5/Controls/RatingStarsPicker.xaml.cs`, метод `OnStarClick`

Последовательность:
1. Берется номер звезды из `Button.Tag`.
2. Вычисляется `newVal` (ограничивается `MaxRating`).
3. Поднимается `PreviewRatingChangingEvent` (Tunnel).
4. Если `Handled=true`, изменение отменяется.
5. Присваивается `Rating = newVal` (с учетом `CoerceRating`).
6. Поднимается `RatingChangingEvent` (Bubble).
7. Поднимается `RatingChangedDirectEvent` (Direct).

Важно: текущий UI звезд дает в основном целые значения, но coercion сохраняется как защита и нормализация данных.

## 3.3 Где демонстрируется разница маршрутов
Файл: `lab4-5/MainWindow.xaml.cs`
- метод `AttachRoutingHandlers()`;
- подписки на события есть на двух уровнях:
  - `Window` (`AddHandler(...)`)
  - контейнер `RoutingHost` (`RoutingHost.AddHandler(...)`).

Это позволяет увидеть:
- Tunnel: прохождение сверху вниз (`Window -> Host -> Source`);
- Bubble: прохождение снизу вверх (`Source -> Host -> Window`).

Где вызывается подключение:
- `AttachRoutingHandlers()` вызывается в `OnLoaded(...)` в `MainWindow.xaml.cs`.

Где находится `RoutingHost`:
- контейнер в `lab4-5/MainWindow.xaml` (`StackPanel x:Name="RoutingHost"`), в нем же находятся оба пользовательских контрола.

## 3.4 Как выводится лог маршрутизации
Файл: `lab4-5/MainWindow.xaml.cs`
- метод `WriteRouting(...)` формирует строку с:
  - номером шага;
  - типом (`Tunnel`/`Bubble`);
  - местом обработки (`Window`/`Host`);
  - именем routed-события;
  - `Source` и `OriginalSource`;
  - данными изменения (старое/новое значение).

Лог отображается в `RoutingLogText` в `MainWindow.xaml`.

Дополнительно для демонстрации:
- лог сделан накопительным (очередь последних записей);
- есть кнопка `Clear routing log` в `MainWindow.xaml`, обработчик `OnClearRoutingLogClick(...)` в `MainWindow.xaml.cs`.

## 3.5 Где видно `Direct`
`Direct` не идет по дереву `Window/Host`, он обрабатывается в самом контроле:
- `DiscountBarControl.OnDirect(...)` -> вывод в `DirectText`;
- `RatingStarsPicker.OnDirect(...)` -> вывод в `DirectText`.

Почему `Direct` не виден в общем логе `Window/Host`:
- в `AttachRoutingHandlers()` на уровне окна/хоста подписки сделаны на Tunnel/Bubble события;
- Direct-события подписаны внутри самих контролов (`AddHandler(...DirectEvent, OnDirect)`).

## 3.6 Карта RoutedEvent: кто объявляет, кто поднимает, кто слушает, где видно

### Discount flow
- Объявляет: `DiscountBarControl.xaml.cs`
  - `PreviewDiscountChangingEvent` (Tunnel)
  - `DiscountChangingEvent` (Bubble)
  - `DiscountChangedDirectEvent` (Direct)
- Поднимает: `DiscountBarControl.OnSliderValueChanged(...)`
- Слушают:
  - `MainWindow` (`Window` и `RoutingHost`) через `AttachRoutingHandlers()` для Tunnel/Bubble
  - `DiscountBarControl` через `OnDirect(...)` для Direct
- Видно в UI:
  - общий лог: `RoutingLogText` (`MainWindow.xaml`)
  - direct-лог контрола: `DirectText` (`DiscountBarControl.xaml`)

### Rating flow
- Объявляет: `RatingStarsPicker.xaml.cs`
  - `PreviewRatingChangingEvent` (Tunnel)
  - `RatingChangingEvent` (Bubble)
  - `RatingChangedDirectEvent` (Direct)
- Поднимает: `RatingStarsPicker.OnStarClick(...)`
- Слушают:
  - `MainWindow` (`Window` и `RoutingHost`) через `AttachRoutingHandlers()` для Tunnel/Bubble
  - `RatingStarsPicker` через `OnDirect(...)` для Direct
- Видно в UI:
  - общий лог: `RoutingLogText` (`MainWindow.xaml`)
  - direct-лог контрола: `DirectText` (`RatingStarsPicker.xaml`)

---

## 4) Использование контролов в проекте

Файл: `lab4-5/MainWindow.xaml`

Оба контрола размещены в правой панели карточки товара:
- `RatingStarsPicker`:
  - `Rating="{Binding Rating, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"`
  - `MaxRating="5"`
- `DiscountBarControl`:
  - `DiscountPercent="{Binding DiscountPercent, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"`
  - `SnapStep="1"`
  - `BarHeight="16"`

Ключевая привязка:
- `DockPanel` с деталями использует `DataContext="{Binding SelectedProduct}"`;
- значит `Rating` и `DiscountPercent` берутся из `SelectedProduct`.

Модель:
- `lab4-5/Models/GuitarProduct.cs`
  - `public double Rating { get; set; }`
  - `public decimal DiscountPercent { get; set; }`

Дополнительные места использования свойств модели:
- `lab4-5/Views/AddProductWindow.xaml`
  - поля ввода `Rating` и `DiscountPercent` для нового товара;
- `lab4-5/Services/ProductRepository.cs`
  - стартовые данные с заполненными `Rating/DiscountPercent`;
- `lab4-5/Models/GuitarProduct.cs`
  - `PriceWithDiscount` рассчитывается на основе `DiscountPercent`.

Итог: пользовательские контролы реально интегрированы в основной экран и работают через биндинги MVVM.

---

## 5) Пользовательская команда на RoutedUICommand

### 5.1 Объявление команды
Файл: `lab4-5/Commands/ShopCommands.cs`
- `ShowRoutingInfo` объявлена как `RoutedUICommand`;
- добавлен `KeyGesture(Key.F1)`.

### 5.2 Привязка команды к окну
Файл: `lab4-5/MainWindow.xaml`
- `Window.CommandBindings` содержит:
  - `Command="{x:Static cmd:ShopCommands.ShowRoutingInfo}"`
  - `Executed="OnRoutingInfoExecuted"`.

### 5.3 Использование команды в UI
Файл: `lab4-5/MainWindow.xaml`
- пункт меню `MenuHelp -> MenuRoutingInfo` вызывает `ShowRoutingInfo`;
- для пользователя показан хоткей `F1`.

### 5.4 Обработчик выполнения
Файл: `lab4-5/MainWindow.xaml.cs`
- метод `OnRoutingInfoExecuted(...)` показывает `MessageBox` с пояснением про `Tunnel/Bubble/Direct`.

---

## Дополнительно: служебные файлы для событий

Файл: `lab4-5/Controls/ShopRoutedEventArgs.cs`
- `ShopDoubleRoutedEventArgs` и `ShopDecimalRoutedEventArgs`;
- используются для передачи `OldValue` и `NewValue` в routed-событиях.

---

## Как быстро проверить работу всех пунктов

1. Запустить приложение.  
2. Выбрать товар в таблице.  
3. Изменять рейтинг (звезды) и скидку (слайдер).  
4. Смотреть:
   - общий лог маршрутизации (`RoutingLogText`) для `Tunnel/Bubble`;
   - `DirectText` внутри контролов для `Direct`.  
5. Нажать пункт помощи в меню или `F1` и убедиться, что срабатывает `RoutedUICommand`.

---

## Краткое соответствие «пункт задания -> файл»

- П.1 (2 UserControl):  
  `Controls/DiscountBarControl.xaml(.cs)`, `Controls/RatingStarsPicker.xaml(.cs)`
- П.2 (DP + Validate/Coerce):  
  `Controls/DiscountBarControl.xaml.cs`, `Controls/RatingStarsPicker.xaml.cs`
- П.3 (RoutedEvent + маршруты + демонстрация):  
  `Controls/*.xaml.cs` (объявление/поднятие), `MainWindow.xaml.cs` (подписки и лог), `MainWindow.xaml` (визуализация лога)
- П.4 (использование в проекте):  
  `MainWindow.xaml`, `Models/GuitarProduct.cs`
- П.5 (RoutedUICommand):  
  `Commands/ShopCommands.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`

---

## Полная карта "свойство -> где объявлено -> где используется"

### `DiscountBarControl.DiscountPercent`
- Объявлено: `Controls/DiscountBarControl.xaml.cs`
- Использование внутри контрола:
  - текст процента (`PercentText`)
  - синхронизация `Slider`
  - отрисовка полосы (`FillRect.Width`)
  - routed events (`old/new`)
- Использование снаружи:
  - биндинг в `MainWindow.xaml` к `SelectedProduct.DiscountPercent`
  - модель `Models/GuitarProduct.cs`
  - репозиторий стартовых данных `Services/ProductRepository.cs`

### `DiscountBarControl.SnapStep`
- Объявлено: `Controls/DiscountBarControl.xaml.cs`
- Использование:
  - `CoerceDiscount(...)` (шаг округления скидки)
  - задается в `MainWindow.xaml` как `SnapStep="1"`

### `DiscountBarControl.BarHeight`
- Объявлено: `Controls/DiscountBarControl.xaml.cs`
- Использование:
  - привязано к `Border.Height` в `Controls/DiscountBarControl.xaml`
  - задается в `MainWindow.xaml` как `BarHeight="16"`

### `RatingStarsPicker.Rating`
- Объявлено: `Controls/RatingStarsPicker.xaml.cs`
- Использование внутри контрола:
  - окраска звезд в `PaintStars()`
  - routed events (`old/new`)
- Использование снаружи:
  - биндинг в `MainWindow.xaml` к `SelectedProduct.Rating`
  - модель `Models/GuitarProduct.cs`
  - ввод при добавлении в `Views/AddProductWindow.xaml`
  - стартовые данные `Services/ProductRepository.cs`

### `RatingStarsPicker.MaxRating`
- Объявлено: `Controls/RatingStarsPicker.xaml.cs`
- Использование:
  - ограничение значения `Rating` (`CoerceRating`)
  - ограничение выбора по клику (`Math.Min(star, MaxRating)`)
  - задается в `MainWindow.xaml` как `MaxRating="5"`
