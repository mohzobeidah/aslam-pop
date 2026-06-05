# Sector Model — Columns

| Column | Type | Description |
|---|---|---|
| Id | int | Primary key |
| Name | string (required) | Sector name (e.g., A, B, C, D) — unique index |
| Camp | string? | Camp name |
| Coordinate | string? | GPS or grid coordinate |
| Area | string? | Area description |
| ManufacturedTentsCount | int | Number of manufactured tents |
| HandmadeTentsCount | int | Number of handmade tents |
| BathroomsCount | int | Number of bathrooms |
| Admins | ICollection\<Admin\> | Navigation — mandoobs assigned to this sector |
