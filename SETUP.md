# Ghid setup local — Budget Planner

## Ce ai nevoie

- [Visual Studio 2022](https://visualstudio.microsoft.com/) cu workload-ul **.NET Multi-platform App UI development**
- [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) (orice editie — Express e gratuita) sau SQL Server LocalDB (vine inclus cu Visual Studio)
- [SQL Server Management Studio (SSMS)](https://aka.ms/ssmsfullsetup)

---

## Pasul 1 — Cloneaza repository-ul

```bash
git clone <url-repo>
cd Proiect_PDM
```

---

## Pasul 2 — Creaza baza de date

1. Deschide **SSMS** si conecteaza-te la serverul tau local:
   - Daca ai **SQL Server Express**: `localhost\SQLEXPRESS`
   - Daca ai **LocalDB**: `(localdb)\MSSQLLocalDB`
   - Daca ai **SQL Server Developer/Standard**: `localhost`

2. In SSMS: **File → Open → File** → selecteaza fisierul `database_setup.sql` din radacina proiectului

3. Apasa **F5** (Execute)

4. Daca totul a mers bine, vei vedea mesajul:
   ```
   BudgetPlannerDB creat cu succes!
   ```

---

## Pasul 3 — Configureaza connection string-ul

Connection string-ul implicit din aplicatie este:
```
Server=localhost;Database=BudgetPlannerDB;Trusted_Connection=True;TrustServerCertificate=True;
```

Daca serverul tau are un alt nume (ex. `localhost\SQLEXPRESS`), va trebui sa il schimbi dupa primul start al aplicatiei:

1. Porneste aplicatia
2. Mergi la sectiunea **Setari** din meniul lateral
3. La sectiunea **Conexiune baza de date**, modifica campul cu connection string-ul corect:

| Tip server | Connection string |
|---|---|
| SQL Server Express | `Server=localhost\SQLEXPRESS;Database=BudgetPlannerDB;Trusted_Connection=True;TrustServerCertificate=True;` |
| LocalDB | `Server=(localdb)\MSSQLLocalDB;Database=BudgetPlannerDB;Trusted_Connection=True;TrustServerCertificate=True;` |
| SQL Server local (default) | `Server=localhost;Database=BudgetPlannerDB;Trusted_Connection=True;TrustServerCertificate=True;` |

4. Reporneste aplicatia dupa ce salvezi

---

## Pasul 4 — Ruleaza proiectul

Deschide `Proiect_Planificare_Buget.sln` in Visual Studio 2022, selecteaza profilul **Windows Machine** si apasa **F5**.

Sau din terminal:
```bash
dotnet run -f net10.0-windows10.0.19041.0 --project Proiect_Planificare_Buget
```

---

## Probleme frecvente

**Eroare de conexiune la pornire**
Aplicatia nu gaseste SQL Server-ul. Verifica connection string-ul din Setari (Pasul 3).

**"Login failed for user"**
Serverul nu accepta Windows Authentication. Adauga utilizatorul curent in SQL Server:
```sql
-- Ruleaza in SSMS ca administrator
CREATE LOGIN [DOMAIN\NumeTau] FROM WINDOWS;
ALTER SERVER ROLE sysadmin ADD MEMBER [DOMAIN\NumeTau];
```

**"Cannot open database BudgetPlannerDB"**
Baza de date nu a fost creata. Reia Pasul 2.

**Fontul pentru iconite nu apare**
Asigura-te ca fisierul `MaterialSymbols.ttf` exista in `Proiect_Planificare_Buget/Resources/Fonts/`. Daca nu, descarca **Material Symbols Rounded** de pe [fonts.google.com/icons](https://fonts.google.com/icons), redenumeste fisierul `.ttf` in `MaterialSymbols.ttf` si pune-l in acel folder.
