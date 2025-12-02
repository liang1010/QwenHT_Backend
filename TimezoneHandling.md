# Timezone Handling in QwenHT Application

## Overview
This document describes the timezone handling implementation in the QwenHT application to ensure all date/time values are stored consistently in UTC.

## Implementation Details

### 1. Backend (.NET 8)

#### DateTime Configuration
- All DateTime properties that represent timestamps now inherit from `BaseEntity` which provides consistent CreatedAt/LastUpdated properties
- Default values are set to `DateTime.UtcNow` in the models
- Entity Framework is configured to use `GETUTCDATE()` SQL function for default values in the database

#### JSON Serialization
- Custom JSON converters (`UtcJsonConverter`, `NullableUtcJsonConverter`) ensure all DateTime values are serialized in UTC format
- These converters are registered in `Program.cs` under JSON serialization options

#### Database Configuration
- Timestamp fields (CreatedAt, LastUpdated) use `HasDefaultValueSql("GETUTCDATE()")` to ensure database-level UTC storage
- This handles cases where direct SQL inserts occur outside the application

### 2. Frontend (Angular)

#### Handling DateTime Values
- When receiving DateTime values from the API, they will be in UTC format
- Display logic should consider the user's local timezone for presentation
- When sending DateTime values to the API, they should be converted to UTC

### 3. Utility Methods

The `TimezoneHelper` class provides utility methods for date/time handling:

```csharp
// Convert any DateTime to UTC
TimezoneHelper.ToUtcDateTime(dateTime);

// Convert UTC DateTime to a specific timezone for display
TimezoneHelper.ConvertUtcToTimezone(utcDateTime, "Asia/Kuala_Lumpur");

// Get current UTC time
TimezoneHelper.GetCurrentUtcTime();
```

## Best Practices

1. Always store DateTime values in UTC in the database
2. Use the `BaseEntity` class for all entities that require timestamp tracking
3. When creating new DateTime values, use `DateTime.UtcNow` instead of `DateTime.Now`
4. For date-only values, use `DateOnly` type instead of `DateTime`
5. When displaying dates to users, convert from UTC to their local timezone
6. When receiving date input from users, convert to UTC before storing

## Migration Considerations

When working with existing data:
- Existing DateTime values may not be in UTC; consider a migration script if needed
- Test thoroughly to ensure all display logic handles UTC properly

## Testing

Ensure to test timezone handling in scenarios such as:
- Creating new records with timestamps
- Updating existing records
- Displaying historical data
- Working across different user timezones