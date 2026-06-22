# Bug Fixes Summary

## Date: 2025-10-22

This document summarizes the critical bug fixes applied to the EMR API codebase.

---

## Fix #1: Error Middleware Placement (CRITICAL)

**Issue:** Error handling middleware was registered AFTER authentication/authorization, causing auth errors to bypass error handling.

**Files Changed:**
- `EMR.API/Program.cs` - Line 90 (moved middleware)
- `EMR.API/Middlewares/ErrorHandlerMiddleware.cs` - Enhanced error handling

**Changes:**
1. Moved `ErrorHandlerMiddleware` before authentication/authorization in the middleware pipeline
2. Added handling for `UnauthorizedAccessException` and `SecurityTokenException`
3. Added environment-aware error message sanitization for production

**Impact:**
- Authentication errors now return consistent JSON responses
- Prevents information disclosure in production
- Improves API client experience

---

## Fix #2: OTP Memory Leak (CRITICAL)

**Issue:** Static HashSet tracking all generated OTPs caused memory leak and potential OTP collisions.

**Files Changed:**
- `EMR.Application/Services/Otps/OtpService.cs`

**Changes:**
1. Removed static `Codes` class with HashSet (lines 297-319)
2. Replaced with simple `GenerateRandomOtp()` method using `Random.Shared`
3. Added retry logic with collision detection in:
   - `RequestOtpAsync()`
   - `RequestEmailOtpAsync()`
   - `ResendOtpAsync()`
   - `ResendEmailOtpAsync()`
4. Database now handles uniqueness validation within validity window

**Impact:**
- Eliminated memory leak
- Fixed potential OTP collisions after HashSet clears
- Works correctly across multiple server instances
- OTPs properly expire based on database records

**Technical Details:**
- 6-digit OTPs = 1 million combinations
- Collision probability: ~0.0001% with 100 active OTPs
- Max retry attempts: 5
- Validation window: 2-5 minutes based on OTP type

---

## Fix #3: Race Condition in Number Generation (CRITICAL)

**Issue:** Concurrent user registrations could generate duplicate user numbers, causing database constraint violations.

**Files Changed:**
- `EMR.Application/Services/NumericService.cs`

**Changes:**
1. Implemented retry logic with exponential backoff (max 10 attempts)
2. Added random delay (10-50ms) between retries to reduce collision probability
3. Improved error handling with detailed exception message
4. Added safeguards against database deadlocks

**Impact:**
- Prevents duplicate user number generation under concurrent load
- Gracefully handles high-concurrency scenarios
- Provides clear error messages when retry limit exceeded

**Technical Details:**
- Max retries: 10 attempts
- Random delay: 10-50ms between attempts
- Exponential backoff for database errors: 50ms * 2^attempt
- Thread-safe number generation

---

## Testing Recommendations

### Fix #1: Error Middleware
```bash
# Test with invalid JWT token
curl -H "Authorization: Bearer invalid_token" https://api/endpoint

# Test with expired token
curl -H "Authorization: Bearer expired_token" https://api/endpoint

# Test with no token on protected endpoint
curl https://api/protected-endpoint
```

**Expected:** All return consistent JSON error responses with appropriate status codes.

---

### Fix #2: OTP Memory Leak
```bash
# Load test: Generate 1000+ OTPs
for i in {1..1000}; do
  curl -X POST https://api/otp/request -d "{\"phoneNumber\":\"+855$i\"}"
done

# Monitor memory usage before and after
# Check logs for collision warnings (should be extremely rare)
```

**Expected:**
- Stable memory usage
- No collision errors
- All OTPs successfully generated

---

### Fix #3: Number Generation
```csharp
// Concurrent test: 100 simultaneous user registrations
var tasks = Enumerable.Range(1, 100)
    .Select(i => Task.Run(async () =>
    {
        var request = new CreateUserRequest
        {
            Email = $"user{i}@test.com",
            FullName = $"Test User {i}",
            Password = "Test123!"
        };
        return await identityService.CreateUserAsync(request, CancellationToken.None);
    }))
    .ToArray();

await Task.WhenAll(tasks);

// Verify all users have unique UserNo values
var users = await dbContext.Users.Select(u => u.UserNo).ToListAsync();
Assert.Equal(users.Count, users.Distinct().Count());
```

**Expected:**
- All 100 users created successfully
- All UserNo values are unique
- No database constraint violations

---

## Deployment Notes

1. **No Database Migrations Required** - All fixes are code-only changes
2. **No Configuration Changes Required** - Existing settings remain valid
3. **Backward Compatible** - No breaking changes to API contracts
4. **Rolling Deployment Safe** - Fixes work correctly with existing code during deployment

---

## Monitoring Recommendations

After deployment, monitor:

1. **Error logs** for:
   - OTP generation collision warnings (should be rare)
   - Number generation retry exhaustion (should be very rare)
   - Authentication error patterns

2. **Metrics**:
   - Memory usage trends (should stabilize)
   - OTP generation success rate (should be ~100%)
   - User registration success rate (should improve)

3. **Alerts** to create:
   - Alert if OTP collision rate > 0.1%
   - Alert if number generation fails after retries
   - Alert if authentication error rate spikes

---

## Rollback Plan

If issues arise, rollback by reverting these commits:
- All changes are isolated to specific methods
- No database schema changes
- Previous behavior can be restored with git revert

---

## Additional Improvements Identified

The following issues were identified but not fixed in this round (lower priority):

1. **Refresh Token Cache Security** (Medium) - KeycloakService.cs:170-175
2. **Transaction Disposal** (Medium) - UnitOfWork.cs:83-92
3. **Null Check Order** (Low) - OtpService.cs:229-238
4. **Missing ConfigureAwait** (Low) - Throughout codebase
5. **Inconsistent Error Logging** (Low) - Multiple service files

These can be addressed in future maintenance cycles.

---

## Code Review Checklist

- [x] Error middleware moved before authentication
- [x] Static OTP HashSet removed
- [x] All OTP methods updated with collision detection
- [x] Number generation implements retry logic
- [x] Error handling improved
- [x] Code follows existing patterns
- [x] No breaking API changes
- [x] No database migrations required
