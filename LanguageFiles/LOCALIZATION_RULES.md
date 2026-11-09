# ModPlus — правила работы с файлами локализации

Эти правила обязательны для любого ИИ и человека, который добавляет, переводит или правит строки в файлах локализации ModPlus (папка `LanguageFiles` с подпапками языков). Они относятся к плагинам для Autodesk Revit, AutoCAD, nanoCAD и Renga. Правила самодостаточны: всё, что нужно для работы с этими файлами, описано здесь.

Если в задаче явно сказано иначе, прямое указание пользователя важнее этого файла.

---

## 1. Источник и объём перевода

- Исходный язык — **русский**. Английский служит для сверки: по нему видно смысл строки, какие идентификаторы не переводятся и какие адаптации сделаны осознанно (например, «ModPlus ЕСКД» → «ModPlus ISO»). Если русский и английский расходятся по смыслу, прав русский. Осознанную адаптацию из английского повторять в остальных языках.
- Перевод пишется сразу на **все языки** из раздела 3. У каждого плагина должен быть файл на каждый язык, а в нём — каждый ключ. Подставлять английский или русский текст вместо перевода нельзя: автоматическая проверка такое не ловит, а пользователь видит.
- Если пользователь просит один конкретный язык — делать только его.
- Если платформа не указана, считать, что речь о плагине для Revit.

## 2. Терминология

### Приоритет источников
1. Прямое указание пользователя в текущей задаче.
2. **Названия плагинов, команд и элементов интерфейса ModPlus**, которые уже есть в этих файлах (`LocalName` плагинов, строки узла `Common`). Заново их не переводить.
3. **Термины платформы** — так, как они написаны в интерфейсе Autodesk на целевом языке (раздел 6).
4. Глоссарий и терминологические таблицы этого файла (разделы 7–8).

Существующие переводы терминов платформы в этих файлах делали люди, и часть из них неточна: совпадение с ними ничего не доказывает. Если перевод в файлах расходится с интерфейсом Autodesk, прав интерфейс, а о расхождении нужно сообщить (раздел 2, «Сомнения»).

### Лексика
- Для одного исходного понятия — один термин. Синонимы не смешивать (например, не писать для одного термина то «nested», то «sub-elements»).
- **Немецкий**: Aktualisierungen (не Updates), Veröffentlichungen (не Releases), Registerkarte (не Tab). Немецкие слова предпочтительнее англицизмов.
- **Испанский**: complementos (не plugins); Funciones de modificación (не Funciones modificadoras).
- Непереводимые сокращения заменять местными эквивалентами. Марка КЖ: S/SC (en), TW (de), EST (es), 结施 (zh).
- ГОСТ, СП, СНиП, ДСТУ, СТБ, ЕСКД — как в английской версии (GOST, SP, SNiP, DSTU, STB; ЕСКД — как адаптировано в en).
- «Контекстное меню» переводить дословно от русского (pt-BR — menu de contexto), а не официальным названием Autodesk (menu de atalho / shortcut menu).
- Префиксы параметров квартирографии КВ_ (Квартира) и ПО_ (Помещение) заменять сокращениями слов целевого языка (раздел 7).

### Сомнения
- В файлы пишется **один вариант** перевода — лучший. Альтернатив и комментариев внутрь строк не вставлять.
- Сомнительные места ИИ перечисляет **отдельным списком в ответе после работы**: файл и ключ, короткая причина. Перевод при этом всё равно делается. В список попадают:
  - переводы на языки, для которых нет интерфейса Autodesk (uk, uz): их нужно проверить носителю;
  - термины, которые не удалось подтвердить в интерфейсе или справке Autodesk;
  - расхождения с уже существующим переводом в файлах;
  - строки, смысл которых неясен ни по русскому, ни по английскому.
- Если сомнений нет, список не выводить. Молча придуманный перевод хуже честной пометки: ошибку в китайском или испанском могут не заметить годами.

### Русский текст
- ИИ исправляет в русских строках только **явные ошибки**: опечатки, «ё» → «е», написание форматов (`XML-файл`), типографские кавычки → прямые. Каждое исправление перечисляется в ответе.
- Смысл и формулировки русского текста без указания пользователя не менять: автор русского текста — разработчик.
- В ru-RU не используется буква «ё» — везде «е».

---

## 3. Языки

| Папка | Суффикс файла | Интерфейс Autodesk на этом языке |
|---|---|---|
| ru-RU | ru | есть |
| uk-UA | uk | нет |
| en-US | en | есть |
| de-DE | de | есть |
| es-ES | es | есть |
| zh-CN | zh | есть |
| fr-FR | fr | есть |
| pt-BR | pt | есть (бразильская версия, не европейская) |
| uz-Latn-UZ | uz | нет; узбекский — латиницей |

- Список растёт. Перед работой сверить его с папками языков в `master`.
- Новый язык: создать папку с полным кодом культуры, файлы с коротким суффиксом, добавить язык в перечисление языков в узле `ModPlusAPI` файла `Common.xml`.

---

## 4. Устройство файлов

### Папка
```
LanguageFiles/
  LanguageFiles.projitems         <- реестр всех файлов; новые файлы вносить сюда
  Langs.xml                       <- версия пакета локализации и список языков
  Special symbols.txt             <- шпаргалка по экранированию
  ru-RU/ en-US/ ... uz-Latn-UZ/   <- папка на каждый язык
    Common.xml                    <- узел Common (общие строки) и узлы общих модулей
    Revit_mprAlignViews_ru.xml    <- файл одного плагина
    AutoCAD_mpPrint_ru.xml
    nanoCAD_mncSumArea_ru.xml
    Renga_mrBatchSheetExport_ru.xml
```

- Имя файла плагина: `Программа_ИмяПлагина_суффикс.xml`. Программа: `Revit`, `AutoCAD`, `nanoCAD` (плагины `mnc*`), `Renga`. Папка называется полным кодом культуры (`uz-Latn-UZ`), суффикс файла — короткий (`uz`).
- Общих файлов `Revit.xml`, `AutoCAD.xml`, `Renga.xml` быть не должно: каждый плагин живёт в своём файле. Исключение — `Common.xml`, он без суффикса во всех языках.
- Новый файл обязательно внести в `LanguageFiles.projitems` рядом с файлами той же папки языка.
- Кодировка UTF-8 с BOM, концы строк CRLF, без перевода строки в конце файла. Сохранять оформление существующего файла: отступы (пробелы или табуляция), расположение атрибутов, XML-комментарии.

### Узел плагина
```xml
<?xml version="1.0" encoding="utf-8"?>
<ModPlus>
  <mpPrint
    LocalName="Печать"
    Description="Пакетная печать нескольких листов, в том числе из модели">
    <h1>Принтер</h1>
    <msg1>Не отмечено ни одной области печати</msg1>
  </mpPrint>
</ModPlus>
```

- Имя узла плагина обязано совпадать с `ModPlusConnector.Name` плагина: по нему ModPlusAPI находит строки.
- Название и описание — **атрибуты** `LocalName`, `Description` (необязательный — `FullDescription`), а не дочерние узлы. `LocalName` — кратко, с заглавной буквы только в начале («Alignement des vues», а не «Alignement Des Vues»).
- Дочерние узлы — строки интерфейса. Принятые префиксы: `h` — надписи и заголовки, `msg` — сообщения и запросы командной строки, `t` — короткие варианты в списках, `err` — ошибки. В существующих плагинах встречаются и другие префиксы (`p1`…) — их не переименовывать.
- Нумерация ключей сквозная и не переиспользуется: освободившийся номер остаётся занятым, иначе переводы на других языках встанут не на свои места.
- Новые строки добавлять в конец узла плагина.
- Имя ключа не может быть пустым и не может начинаться с цифры. Пустые значения и повторяющиеся ключи — ошибка.
- Набор файлов, узлов, ключей и атрибутов должен совпадать во всех языках. Ключ, добавленный только в русский файл, — пустое место в интерфейсе на остальных языках.

### Экранирование
- Строки — это сырой экранированный XML-текст. Сохранять экранирование точно как в исходнике: `&quot;` (двойная кавычка), `&#x0a;` (перевод строки), `&lt;`, `&gt;`, `&amp;`, литерал `\n`.
- Голые `<` и `&` не вводить. Внутри значений атрибутов не вводить голую `"`, только `&quot;`.
- В тексте элементов кавычки остаются неэкранированными, как набраны в файле.

---

## 5. Common.xml

`Common.xml` — общий файл, около 2600 строк. Он без языкового суффикса во всех языках и состоит из нескольких узлов:

| Узел | Что в нём |
|---|---|
| `Common` | Общие строки всех плагинов (около 220): `cancel`, `accept`, `apply`, `add`, `close`, `view`, `warning`, `configuration`… |
| `ModPlusAPI`, `Core` | Сообщения библиотеки ModPlusAPI и ядра; в `ModPlusAPI` — перечисление языков (`ru-RU`, `en-US`…) |
| `Geometry`, `Style`, `CUI`, `NotesCore` | Строки общих модулей: геометрия, выбор цвета, вкладки ленты (`group1`…), примечания |
| `Configurator`, `AutoUpdater`, `AutocadDlls`, `nanocadDlls`, `RevitDlls`, `RengaDlls` | Служебные программы и рабочие библиотеки платформ (с `LocalName`) |
| `ReinforcementBendRules`, `ReinforcementCoupler`, `RengaEntityTypes`, `RengaQuantities`, `RengaParameters` | Справочники |

### Узел Common
- **Перед добавлением строки в файл плагина искать её в узле `Common` файла `ru-RU/Common.xml`** по русскому значению. Найденное в файл плагина не дублировать: в коде оно привязывается к общему ключу (раздел 9). Своя копия «Отмена» в плагине — лишняя работа переводчикам и расхождения между плагинами.
- **Если общей строки в `Common` нет — добавить её туда**, а не в файл плагина. Общая строка — та, что не привязана к конкретному плагину и пригодится в других: подписи кнопок и действий, общие понятия и единицы («Удалить все», «Экспорт», «Высота»). Строки, смысл которых зависит от плагина, остаются в файле плагина.
- Ключ — слово или словосочетание на английском в camelCase по смыслу строки (`removeAll`, `export`, `height`), без номеров. Перед добавлением проверить, что такого ключа ещё нет.
- Новую строку вставлять **по алфавиту ключа**, а не в конец узла: найти место среди соседних ключей (без учёта регистра) и вставить туда. Существующий порядок не пересортировывать — часть ключей уже стоит не по алфавиту.
- Строка добавляется во все языки, на одно и то же место.
- Все добавленные в `Common` строки перечислить в ответе.

### Остальные узлы
- В другие узлы `Common.xml` строки добавлять **только когда задача касается этого модуля или библиотеки** (например, правка `RevitDlls` или `ModPlusAPI`). Строки плагинов туда не класть.
- Названия языков в `ModPlusAPI` (`ru-RU`, `en-US`, `de-DE`, `uk-UA`, `es-ES`, `zh-CN`, `fr-FR`, `pt-BR`, `uz-Latn-UZ`) не переводить: каждый язык записан на своём языке. Новый язык добавлять в это перечисление.
- Вкладку ленты `CUI/group3` («Утилиты») переводить так же, как плагин mprTools: fr — Outils, pt-BR — Utilitários.
- Повторы ключей внутри любого узла — ошибка проверки, их нужно удалять.

---

## 6. Оформление строк

### Что не переводить
- Плейсхолдеры `{0}`, `{1}`… Набор сохранять тот же; порядок может меняться под грамматику языка.
- Имена функций и параметров, системные идентификаторы и имена узлов конфигурации (`Plugins`, `Prohibited`).
- Расширения и названия форматов файлов, единицы измерения (mm, cm, m, m², kg).
- Названия продуктов: ModPlus, Revit, AutoCAD, nanoCAD, Renga, Excel, Navisworks, IFC, DWG, PDF.
- Если русская строка совпадает с английской и это явно код, число или формат — копировать без изменений.
- Ключи h41–h46 в mpSpecifications остаются на русском во всех языках: это ключи.

### Пунктуация и типографика
- Пунктуацию сохранять как в исходнике: точку в конце не добавлять, если её нет, и не убирать, если она есть. Ведущие и замыкающие `:`, `...`, `…`, `-`, `*` и переносы строк оставлять на своих местах.
- Десятичный разделитель в примерах — как в исходнике.
- В испанском ставить перевёрнутые `¿` и `¡`.
- **Кавычки** во всех языках, включая zh-CN, — только прямые двойные `"…"`. Если в русском стоят «», в переводе всё равно `"…"`. Никаких “ ” « » „. Типографские кавычки сборка исправляет в исходниках автоматически, но писать нужно сразу прямые.
- Регистр — как в исходнике (обычно с заглавной только первое слово).
- **DWG** — всегда прописными, во всех языках.
- Названия форматов (XML, DWG, DXF, DWF, PDF, TXT, PAT, BAK, HTML, OXPS, IFC) в сочетании со словом «файл»:

| Язык | Пример |
|---|---|
| ru, uk | `XML-файл`, `PDF-принтер` (через дефис) |
| en | `XML file`, `DWG base` (без дефиса) |
| de | `XML-Datei`, `DWG-Datenbank` |
| es, fr, pt | `archivo XML`, `fichier XML`, `arquivo XML` |
| uz | `XML fayl` (через пробел) |
| zh | `XML文件` (без добавления пробелов) |

  Расширения с точкой (`.xml`), маски (`*.dwg`) и имена файлов не трогать. `DWFx` писать именно так, как у Autodesk.

### Стиль
- Кнопки, команды, заголовки, подписи — инфинитив или существительное («Appliquer», «Aplicar», «Alignement des vues»).
- Подсказки, инструкции и сообщения пользователю — повелительное наклонение, естественно и кратко («Sélectionnez une fenêtre», «Selecione uma janela de visualização»).
- Один и тот же русский текст и термин в пределах работы переводить одинаково.

---

## 7. Решения по языкам

### fr-FR (французский, Франция)
- Обращение на **vous**. Кнопки — инфинитив или существительное, подсказки — повелительное на vous.
- **Неразрывный пробел U+00A0** (сам символ, не сущность) перед `:` `;` `!` `?`, когда они идут после слова в обычном тексте («Aligner :», «Continuer ?»). Не ставить во времени, URL, путях, `{0}:{1}`, координатах `x:y`, коде.
- плагин → plugin; настройки → Réglages («Réglages du plugin»); параметр → paramètre.
- Размеры вдоль линии → Cotes le long d'une ligne.
- полная / ограниченная (бесплатная) версия → version complète / version limitée.
- mprTools («Утилиты») и вкладка group3 → Outils.
- Префиксы КВ_ / ПО_ → AP_ (Appartement) / PI_ (Pièce).
- Сверено со справкой Autodesk FR (Revit/AutoCAD 2025): pin/unpin — Verrouiller/Déverrouiller; routing preferences — préférences d'acheminement; area reinforcement — armature surfacique; solid fill — remplissage uni; Zoom to Fit — Zoom tout; stripe rows — lignes en surbrillance; fit to paper — A la taille du papier; parts (Revit Parts) — éléments (официально, но неоднозначно).

### pt-BR (португальский, Бразилия)
- Бразильская орфография и лексика, не европейская: arquivo, tela, usuário, registro, ação, seleção.
- Кнопки — инфинитив; подсказки, инструкции, сообщения — повелительное на «você» (Selecione, Clique, Marque as folhas…).
- плагин → plugin; настройки → Configurações («Configurações do plugin»); параметр → parâmetro.
- Размеры вдоль линии → Cotas ao longo da linha.
- полная / ограниченная (бесплатная) версия → versão completa / versão limitada.
- Утилиты (mprTools и вкладка group3) → Utilitários.
- квартира → apartamento; квартирография → layout de apartamentos. Префиксы КВ_ / ПО_ → AP_ (Apartamento) / AM_ (Ambiente).
- Слои ModPlus: Mp_Chamadas; Mp_Непечатаемый → Mp_NãoPlotável (одинаково в AutoCAD и nanoCAD).
- Сверено со справкой Autodesk Revit/AutoCAD 2025 PTB: workset и viewport остаются по-английски; армирование по площади — armadura da área, по траектории — armadura do caminho, сеткой — armadura da tela soldada; соединитель арматуры — acoplador de vergalhão; заемщик — usuário temporário; засечки — marca de revisão; обходы — percurso virtual.

### uz-Latn-UZ (узбекский, латиница)
- Только латиница. oʻ/gʻ — через ʻ (U+02BB), тутук белгиси (апостроф) — через ʼ (U+02BC).
- плагин → plagin. Термины Revit переводить на узбекский (Oila, Koʻrinish, Varaq…).
- Стороны света — международные N/S/W/E.
- Имена параметров Revit в примерах подсказок (`{Name}`, `$[Type Name]`) — английские.
- Квартирография → Xonadonlar tarkibi. Префиксы КВ_ / ПО_ → XD_ (Xonadon) / XN_ (Xona).
- Интерфейса Autodesk на узбекском нет: переводы терминов помечать в списке сомнений.

### uk-UA (украинский)
- Интерфейса Autodesk на украинском нет. Опираться на сложившийся перевод в файлах и на русский и английский термины; переводы терминов помечать в списке сомнений.

### Глоссарий для всех языков

| RU | UK | EN | DE | ES | ZH |
|---|---|---|---|---|---|
| плагины | плагіни | plugins | Plugins | complementos | 插件 |
| Размеры вдоль линии | Розміри вздовж лінії | Dimensions by line | Bemaßungen entlang der Linie | Cotas a lo largo de línea | 沿线标注 |

---

## 8. Термины платформ

Перевод должен повторять то слово, которое пользователь видит в самом Revit или AutoCAD на своём языке. Словарный перевод выглядит чужеродно рядом с родными командами и заставляет искать в интерфейсе не то, что написано в плагине.

### Как искать термин
- **ru, en.** Из установленной программы: лента, меню, подсказки, подписи диалогов, запросы командной строки.
- **de, es, zh, fr, pt.** Отталкиваться от английского термина и проверять написание на нужном языке в справке Autodesk (`help.autodesk.com`, версия справки на этом языке; для pt — бразильская, PTB). Официальные китайские названия категорий Revit брать оттуда же. Для fr и pt сначала смотреть таблицы ниже.
- **uk, uz.** Интерфейса Autodesk нет — см. раздел 7.
- Одно слово Autodesk может переводиться по-разному в разных местах (в AutoCAD «Вписать» в диалоге — Fit to paper, в командной строке — Fit). Брать вариант из того же места, где строка стоит в плагине.
- **AutoCAD/nanoCAD: ключевые слова команд.** Переводится только локальное имя, глобальное остаётся английским. Образец — запросы AutoCAD вида `Черновое Просмотр _ Draft Preview`: до `_` локальные имена, после — глобальные.
- **Терминология не смешивается между платформами.** Для плагинов AutoCAD и nanoCAD сверяться с `AutoCAD_*.xml`, `nanoCAD_*.xml` и `Common.xml`; у Revit и Renga для тех же понятий другие слова.
- **Renga.** Нейтральная BIM-лексика целевого языка и, где применимо, те же термины, что для Revit (уровень — niveau / nível, объект — objet / objeto, свойство — propriété / propriedade).

В таблицах «—» значит, что решение по языку не принималось: брать официальный термин Autodesk.

### Revit — официальные термины fr-FR и pt-BR

| RU | FR | PT-BR |
|---|---|---|
| вид | vue | vista |
| план этажа | plan d'étage | planta de piso |
| план потолка | plan de plafond | planta de forro refletido |
| разрез | coupe | corte |
| фасад | élévation | elevação |
| 3D-вид | vue 3D | vista 3D |
| чертежный вид | vue de dessin | vista de desenho |
| легенда | légende | legenda |
| спецификация (Revit schedule) | nomenclature | tabela |
| ведомость | nomenclature/liste | tabela/lista |
| лист | feuille | folha |
| видовой экран | fenêtre | viewport |
| основная надпись | cartouche | bloco de margens e carimbo |
| заголовок вида | titre de la vue | título de vista |
| шаблон вида | gabarit de vue | modelo de vista |
| область подрезки | zone cadrée | região de recorte |
| секущий диапазон | plage de vue | faixa da vista |
| диспетчер проекта | arborescence du projet | navegador de projeto |
| свойства | propriétés | propriedades |
| свойства типа | propriétés du type | propriedades de tipo |
| семейство | famille | família |
| вложенное семейство | famille imbriquée | família aninhada |
| загружаемое семейство | famille chargeable | família carregável |
| системное семейство | famille système | família de sistema |
| редактор семейств | éditeur de familles | editor de família |
| типоразмер | type | tipo |
| экземпляр | occurrence | instância |
| параметр типа / экземпляра | paramètre de type / d'occurrence | parâmetro de tipo / de instância |
| общий параметр | paramètre partagé | parâmetro compartilhado |
| параметр проекта | paramètre de projet | parâmetro de projeto |
| глобальный параметр | paramètre global | parâmetro global |
| категория | catégorie | categoria |
| подкатегория | sous-catégorie | subcategoria |
| уровень | niveau | nível |
| ось (сетка осей) | quadrillage | eixo |
| опорная плоскость | plan de référence | plano de referência |
| рабочая плоскость | plan de construction | plano de trabalho |
| рабочий набор | sous-projet | workset |
| стадия | phase | fase |
| вариант конструкции | variante | opção de projeto |
| связь / связанная модель | lien / modèle lié | vínculo / modelo vinculado |
| группа | groupe | grupo |
| помещение | pièce | ambiente |
| пространство | espace | espaço |
| зона/площадь | surface | área |
| марка | étiquette | identificador |
| ключевая пометка | note d'identification | nota-chave |
| текстовое примечание | note textuelle | nota de texto |
| размер | cote | cota |
| цепочка размеров | chaîne de cotes | cadeia de cotas |
| высотная отметка | cote d'élévation | elevação de ponto |
| материал | matériau | material |
| образец заливки/штриховки | motif de remplissage | padrão de preenchimento |
| образец линии | motif de ligne | padrão de linha |
| стиль линий | style de ligne | estilo de linha |
| вес линий | épaisseur de ligne | espessura de linha |
| видимость/графика | visibilité/graphismes | visibilidade/gráficos |
| фильтр | filtre | filtro |
| переопределение графики | remplacements de graphismes (Remplacer les graphismes…) | sobreposição de gráficos (Sobrepor gráficos na vista) |
| полутона | demi-teinte | meio-tom |
| скрыть | masquer | ocultar |
| стена | mur | parede |
| витраж | mur-rideau | parede cortina |
| импост | meneau | montante secundário |
| панель витража | panneau de mur-rideau | painel de parede cortina |
| перекрытие | sol (несущее: plancher) | piso (structural: piso estrutural) |
| плита | dalle | laje |
| потолок | plafond | forro |
| крыша | toit | telhado |
| колонна | poteau (несущая колонна — poteau porteur) | pilar (несущая колонна — pilar estrutural) |
| балка / каркас | poutre / ossature | viga / Framing estrutural |
| фундамент | fondation | fundação |
| лестница | escalier | escada |
| ограждение | garde-corps | guarda-corpo |
| дверь | porte | porta |
| окно | fenêtre | janela |
| проем/отверстие | ouverture | abertura |
| профиль стены (wall sweep) | profil en relief | extrusão por percurso de parede |
| арматура (Revit rebar element) | armature | vergalhão |
| арматурный стержень | barre d'armature | barra de vergalhão |
| форма арматуры | forme d'armature | forma do vergalhão |
| армирование (process/design, plugin names like "Армирование стен") | ferraillage ("Ferraillage des murs") | armadura ("Armadura de paredes") |
| защитный слой | enrobage | recobrimento |
| хомут | cadre/étrier | estribo |
| анкеровка | ancrage | ancoragem |
| нахлест | recouvrement | traspasse |
| шаг | espacement | espaçamento |
| основа (host) | hôte | hospedeiro |
| труба | canalisation | tubo/tubulação |
| воздуховод | gaine | duto |
| кабельный лоток | chemin de câbles | bandeja de cabos |
| короб | conduit | conduíte |
| соединительная деталь | raccord | conexão |
| арматура трубопроводов | accessoire de canalisation | acessório de tubulação |
| соединитель | connecteur | conector |
| оборудование | équipement | equipamento |
| система | système | sistema |
| врезка | piquage | derivação |
| предупреждение | avertissement | aviso |
| топотело | solide topographique | sólido topográfico |
| линия модели | ligne de modèle | linha de modelo |
| линия детализации | ligne de détail | linha de detalhe |
| элемент узла | composant de détail | componente de detalhe |
| изменение (revision) | révision | revisão |
| сведения о проекте | informations sur le projet | informações do projeto |
| общие координаты | coordonnées partagées | coordenadas compartilhadas |
| базовая точка проекта | point de base du projet | ponto base do projeto |
| точка съемки | point de topographie | ponto de pesquisa |
| истинный север | nord géographique | norte verdadeiro |
| ID элемента | ID d'élément | ID do elemento |
| лента | ruban | faixa de opções |
| вкладка | onglet | guia |
| панель (ленты) | groupe de fonctions | painel |
| файл хранилища | modèle central | modelo central |
| синхронизация | synchronisation | sincronização |
| документ | document | documento |
| модель | modèle | modelo |

### AutoCAD и nanoCAD — официальные термины fr-FR и pt-BR

| RU | FR | PT-BR |
|---|---|---|
| блок | bloc | bloco |
| вхождение блока | référence de bloc | referência de bloco |
| атрибут | attribut | atributo |
| слой | calque | camada |
| лист (layout) | présentation | layout |
| пространство модели | espace objet | espaço do modelo |
| пространство листа | espace papier | espaço do papel |
| видовой экран | fenêtre | viewport |
| полилиния | polyligne | polilinha |
| мультитекст (МТекст) | texte multiligne | texto multilinha |
| однострочный текст | texte | texto de linha única |
| размер | cote | cota |
| размерный стиль | style de cote | estilo de cota |
| таблица | tableau | tabela |
| стиль таблиц | style de tableau | estilo de tabela |
| штриховка | hachure | hachura |
| аннотативный масштаб | échelle d'annotation | escala de anotação |
| порядок прорисовки | ordre de tracé | ordem de desenho |
| примитив/объект | objet | objeto |
| тип линий | type de ligne | tipo de linha |
| вес линий | épaisseur de ligne | espessura de linha |
| командная строка | ligne de commande | linha de comando |
| чертеж | dessin | desenho |
| шаблон | gabarit | modelo (template) |
| внешняя ссылка | Xréf | referência externa (Xref) |
| зеркало | miroir | espelhar |
| подобие (offset) | décaler | deslocamento |
| поворот | rotation | rotação |
| масштаб | échelle | escala |
| расчленить | décomposer | explodir |
| вершина | sommet | vértice |
| сегмент | segment | segmento |
| дуга | arc | arco |
| сеть (mesh) | maillage | malha |
| динамический блок | bloc dynamique | bloco dinâmico |
| ручка | poignée | alça |
| ПСК | SCU | UCS |
| МСК | SCG | WCS |
| штамп (ЕСКД title block) | cartouche | carimbo |
| спецификация (ЕСКД) | nomenclature | lista de materiais |
| выноска | ligne de repère / repère | linha de chamada |
| полка выноски (landing) | ligne de guidage | patamar |
| маскировка (wipeout) | nettoyage | cobertura |
| позиция (marking) | repère | marcação |
| мультивыноска | — | linha de chamada múltipla |

---

## 9. Привязка строк в коде плагина

Отдельный этап, его часто делают позже перевода. К этому моменту ключи уже должны быть в файлах локализации.

1. Пройти по XAML и C# плагина и выписать всё, что видит пользователь: заголовки, подписи, `ToolTip`, тексты кнопок, сообщения `MessageBox` и `Editor.WriteMessage`, ключевые слова командной строки.
2. Общие строки взять из узла `Common` или добавить туда (раздел 5), специфичные добавить в файл плагина на всех языках.
3. XAML — провайдеры в ресурсах окна, `XPath` — имя плагина и `Common`:

```xml
<XmlDataProvider x:Key="Lang" XPath="ModPlus/mpPrint" />
<XmlDataProvider x:Key="LangCommon" XPath="ModPlus/Common" />

<TextBlock Text="{Binding Source={StaticResource Lang}, XPath=h1}" />
<Button Content="{Binding Source={StaticResource LangCommon}, XPath=cancel}" />
```

4. C#: `Language.GetItem("msg1")` — строки плагина, `Language.GetCommonItem("cancel")` — общие.
5. Не собирать фразы из кусков: порядок слов в языках разный. Выносить целое предложение и подставлять значения через плейсхолдеры `{0}`.
6. После правок собрать плагин и пройти по окну глазами, сменив язык: непривязанная строка выглядит так же, как привязанная, пока язык не сменишь.

---

## 10. Git, проверка и публикация

### Ветка
Работать в отдельной ветке от свежего `master`: параллельно локализацию ведут и другие плагины, а в старых ветках нет языков, добавленных позже.

```bash
git -C <repo> fetch
git -C <repo> switch --no-track -c DEV_<плагин>_Localization origin/master
```

- Без `--no-track` новая ветка отслеживает `origin/master`, и обычный push (в том числе кнопкой в IDE) уходит прямо в `master`.
- Слияние в `master` **запускает публикацию локализации** (GitHub Actions): поднимается версия, файлы уходят на сервер и в хранилище. Поэтому в `master` попадает только проверенное, через pull request.
- Ветку на сервер публиковать первым push с `git push -u origin <ветка>`, и только по просьбе пользователя.
- Если ветка старше `master` (в ней нет папки какого-то языка), сначала обновить её от `master`, потом дописывать новые языки.
- Путь к репозиторию на разных машинах разный. Если он неизвестен — спросить, не угадывать.

### Проверка
- Каждый pull request автоматически проверяется утилитой `ModPlus.LanguageTools.Cli` (команда `check`): одинаковая структура файлов, узлов и атрибутов во всех языках, пустые значения, имена ключей, повторы ключей. Повторы наборов значений — предупреждения, публикацию они не блокируют.
- После правок проверить, что каждый изменённый файл разбирается как XML.
- Перед коммитом посмотреть `git diff`: в нём должны быть только нужные строки. Если файл (особенно `.projitems`) изменился целиком, значит, поехали концы строк или кодировка.

### Версия
- Версия пакета хранится в `Langs.xml`. При публикации к второй цифре большей из версий (в репозитории и в хранилище) прибавляется 1, остальные части не меняются (15.0.23.0 → 15.1.23.0). Вручную версию не менять.

### Инструменты
- `sed -i` и `awk` в Git Bash на Windows затирают CRLF во всём файле: правка двух строк превращается в дифф на сотни строк. Для файлов локализации использовать инструменты, которые сохраняют UTF-8 с BOM и CRLF (PowerShell с побайтовой записью, скрипт с явной кодировкой).
- Не рассчитывать, что на машине есть Python или другой интерпретатор: сначала проверить, что установлено.
