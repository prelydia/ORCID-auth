# Авторизация пользователя через ORCID

В интернете почти нет информации об этом, поэтому делюсь своими наработками. Может быть, кому-то пригодится

Для начала нужно зарегистрироваться в песочнице ORCID (url: sandbox.orcid.org), чтобы безопасно тестировать и отлаживать код. После регистрации следует перейти в инстументы разработчика и, заполнив информацию о системе, получить токены (Client Id и Client Secret). 
Для большей безопасности хранить эти токены следует в appsettings.json

```C#
  "OrcidSettings": {
    "ClientId": "YOUR-CLIENT-ID",
    "ClientSecret": "YOUR-CLIENT-SECRET",
    "RedirectUri": "http://localhost:5001/Access",
    "AccessScope": "/read-limited"
  },
```


