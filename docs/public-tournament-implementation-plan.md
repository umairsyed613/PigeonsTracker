# PublicTournament Implementation Plan

## Objective
Implement a new **PublicTournament** flow (parallel to MyTournament) where:
- A tournament can contain multiple lofts.
- Flying day/time is shared across lofts.
- Day records contain per-loft records.
- Two summary pages are supported:
  1. Bird-index matrix (`Bird1`, `Bird2`, ...), plus daily total.
  2. Totals view (landed, not-landed, baby pigeon sum, total hours).
- Data is persisted in Firestore.
- Manager and loft updates are code-gated.

## Confirmed Decisions
- Name is **PublicTournament** (not v2).
- Codes are **digit-only** and formatted `XXX-XXX-XXX`.
- Plain manager/loft/recovery codes are stored in Firestore by design.
- Only manager can regenerate codes.
- Manager recovery key exists to re-assert ownership.
- "Manage loft records" is an explicit user option and flow.
- Summary #1 headers use bird index (`Bird1`, `Bird2`), not bird names.

## Data Model
### PublicTournament
- `FireStoreId`
- `Id`
- `Name`
- `CreatedAt`
- `StartsFrom`
- `EndTo`
- `FlyingStartTime`
- `CanManageLoftRecords`
- `ManagerCode`
- `ManagerRecoveryKey`
- `CodeVersion`
- `LastCodeRegeneratedAt`
- `Lofts[]`
- `DayRecords[]`

### PublicTournamentLoft
- `LoftId`
- `LoftName`
- `BirdCount`
- `HasBabyPigeon`
- `LoftCode`
- `CreatedAt`

### PublicTournamentDayRecord
- `Id`
- `Date`
- `CreatedAt`
- `LoftRecords[]`

### PublicTournamentLoftDayRecord
- `LoftId`
- `LoftName`
- `StartTime`
- `BirdRecords[]`
- `BabyBird`
- `TotalHours`
- `TotalLanded`
- `TotalNotLanded`
- `BabyPigeonSum`
- `UpdatedAt`

### PublicTournamentBirdRecord
- `BirdIndex`
- `EndTime`
- `TotalBirdFlyingTime`

## API Scope (initial implementation)
- `POST /api/publictournament/create`
- `GET /api/publictournament/get/{id}` (by public tournament id)
- `POST /api/publictournament/dayrecord/upsert`
- `POST /api/publictournament/codes/regenerate`
- `GET /api/publictournament/summary/birdindex/{id}`
- `GET /api/publictournament/summary/totals/{id}`

## Authorization Rules
- Public read is allowed.
- Write requires a code:
  - Manager code or manager recovery key => manager role.
  - Matching loft code for requested loft => loft manager role for that loft.
- Only manager role can regenerate codes.

## Summary Rules
### Summary 1 (Bird Index Matrix)
Columns:
- `LoftName`, `DateOfFlying`, `Bird1Hours`...`BirdNHours`, `TotalSumOfDay`

Notes:
- `N` is max bird count among tournament lofts.
- Missing birds for shorter lofts return null/empty values.

### Summary 2 (Totals)
Columns:
- `LoftName`, `DateOfFlying`, `TotalLanded`, `TotalNotLanded`, `BabyPigeonSum`, `TotalHours`

## Delivery Phases
1. Shared model + request/response contracts.
2. Firestore DTOs + mapper + DI wiring.
3. API endpoints + validation + aggregate computation.
4. Blazor service integration.
5. Blazor pages and manage-code UX.
6. Testing and polish.

## Implementation Start in This Change Set
- Add shared PublicTournament contracts.
- Add Firestore PublicTournament models.
- Add mapper + Program DI registration.
- Add API endpoints for create/get/upsert/regenerate/summaries.
