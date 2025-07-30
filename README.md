# TimeRecorder
Celem zadania jest stworzenie aplikacji której zadaniem będzie rejestrowanie czasu pracy.
Rezultatem ma być aplikacja, w ramach której każda osoba z organizacji ma możliwość
rejestracji swojego czasu pracy. W aplikacji mamy możliwość rozpoczęcie pracy, zrobienia przerwy,
wznowienia pracy oraz zakończenia pracy. Opcją dodatkową będzie możliwość dodania/oznaczenia
dnia wolnego/urlopu.
Dodatkowo aplikacja powinna posiadać panel administratora. Administrator ma możliwość
akceptacji dnia wolnego lub jego odrzucenia. Dodatkowo powinien widzieć podsumowanie
dnia/tygodnia/miesiąca w ramach zespołu lub pracownika. Rozwinięciem projektu będzie
wprowadzenie możliwości definicji projektów oraz ich powiązania z API logowania czasu pracy – czyli
rozliczanie czasu pracy według projektów. Aplikacja powinna zawierać podsumowanie dni wolnych.
Wszyscy użytkownicy mogą podglądać kalendarz dni wolnych, czyli sprawdzić kto i kiedy
będzie „na urlopie”.
Dodatkowym zadaniem będzie integracja z kanałem Teams i użycie AI. System powinien na
wskazanym kanale zakładać wątek „Praca w dniu dd.mm.rrrr”. Wątek powinien powstać dzień
przed danym dniem. Wszyscy pracownicy wpisując się, mają możliwość swobodnego wpisania się
z rozpoczęciem, przerwą, bądź zakończeniem pracy. Z pomocą AI interpretujemy treść wpisu
i wywołujemy odpowiednią akcję w naszym API. Kolejnym wyzwaniem będzie wysyłanie
powiadomień na teams, do użytkowników, którzy nie rozpoczęli/zakończyli dnia pracy.
