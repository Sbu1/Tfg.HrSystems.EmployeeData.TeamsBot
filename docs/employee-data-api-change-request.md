# Employee Data API - Change Request (from the In-Office Hours Assistant team)

| Field | Value |
|-------|-------|
| Raised by | In-Office Hours Assistant (Teams bot) team |
| API | `Tfg.HrSystems.EmployeeData` |
| Environment | Test - `https://tst-tfg-hrsystems-employeedata-api.apps.ocptst.ho.fosltd.co.za` |
| Date observed | 2026-07-02 |
| Test subject | Manager employee number `10412592` (Duminy, JD) |
| Priority | High - blocks current-month manager views in the bot |

## Summary

`GET /api/managerteam` returns **no current-month data** for a manager's direct reports, even though the same
manager's own current-month data **is** present on `GET /api/employee`. The two endpoints appear to be populated
on different cadences, so a manager cannot see their team's current-month standing. Separately, the `months`
query parameter's behaviour around the (empty) current month is easy to misread.

## Observed behaviour (evidence)

Today is 2026-07-02 (calendar month 7 = July).

**`GET /api/managerteam?employeeNumber=10412592&months={m}`** - distinct calendar months returned:

| `months` | Calendar months present in the response |
|----------|------------------------------------------|
| 1 | *(empty array)* |
| 2 | 6 (June) |
| 3 | 5, 6 (May, June) |
| 6 | 2, 3, 4, 5, 6 (Feb - June) |

-> `managerteam` contains **no July (month 7) rows for any report**; the most recent team data is June.

**`GET /api/employee?employeeNumber=10412592`** - calendar months present:

`2, 3, 4, 5, 6, 7 (July x2)` -> the manager's **own** record **does** include July (the current month).

So for the same person and period, the per-employee endpoint has July but the manager-team endpoint does not.

## Expected behaviour

1. `GET /api/managerteam` should return the reports' **current-month (T-1)** hours on the **same cadence** as
   `GET /api/employee`. If a manager has current-month data, their reports' current-month data should be
   available at the same time.
2. The two endpoints should be **consistent** for the same period - a report's current-month figure should not
   be visible via one endpoint and missing from the other.

## Requested fixes / additions

1. **(Primary) Populate current-month team-member hours in `GET /api/managerteam`** on the same schedule as the
   per-employee data. Please confirm whether the current gap is expected ETL/aggregation latency or a defect.
2. **Consistency** between `/api/employee` and `/api/managerteam` for the same employee/period.
3. **Clarify / adjust the `months` parameter semantics.** Currently `months=N` appears to count back from the
   current calendar month **including** it, so `months=1` returns an empty array when the current month has no
   data (rather than the most recent month that does). Options:
   - document this behaviour explicitly, and/or
   - skip an empty leading current month, and/or
   - add an optional explicit period filter, e.g. `calendarMonth=YYYYMM`, or a `latestAvailable=true` flag.
4. **(Nice to have) Return a data-currency / "as at" timestamp** (per record or as a response header) so
   consumers can display accurate freshness and detect stale/missing periods.

## Impact on the consumer

The Teams assistant's **"Team this month"** and **"Who's at risk"** manager views cannot show current-month team
standing early in the month - they currently fall back to each report's most recent available month (June/May),
clearly labelled with that month. Employee self-views are unaffected (the per-employee endpoint has the data).

## Reproduction

```bash
BASE="https://tst-tfg-hrsystems-employeedata-api.apps.ocptst.ho.fosltd.co.za"
curl -s "$BASE/api/managerteam?employeeNumber=10412592&months=1"   # -> []
curl -s "$BASE/api/managerteam?employeeNumber=10412592&months=2"   # -> June rows only
curl -s "$BASE/api/managerteam?employeeNumber=10412592&months=6"   # -> Feb..June, no July
curl -s "$BASE/api/employee?employeeNumber=10412592"               # -> includes July (current month)
```
