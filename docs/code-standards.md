# VnStock Platform — Code Standards & Guidelines

**Last Updated:** 2026-03-09
**Status:** Phase 1 Foundation
**Applies to:** .NET Backend, Python Data Service, React Frontend

---

## Table of Contents

1. [General Principles](#general-principles)
2. [.NET Backend Standards](#net-backend-standards)
3. [Python Data Service Standards](#python-data-service-standards)
4. [React Frontend Standards](#react-frontend-standards)
5. [Database Standards](#database-standards)
6. [Security Standards](#security-standards)
7. [Testing Standards](#testing-standards)
8. [Git & Commit Standards](#git--commit-standards)

---

## General Principles

### YAGNI (You Aren't Gonna Need It)
- Write only code needed for current phase
- Don't add "future-proofing" features not in requirements
- Refactor when pattern becomes clear (multiple uses)

### KISS (Keep It Simple, Stupid)
- Prefer straightforward solutions over clever code
- Use standard libraries before inventing custom solutions
- Optimize for readability first, performance second

### DRY (Don't Repeat Yourself)
- Extract repeated logic into helper functions/methods
- Use composition and inheritance appropriately
- Share constants in one location, not scattered throughout

### File Naming Convention

**Rationale:** LLM tools (Grep, Glob) rely on file names to understand purpose

**Rules:**
- **JavaScript/TypeScript:** kebab-case with descriptive names
  - ✓ `auth-store.ts`, `protected-route.tsx`, `api-client.ts`
  - ✗ `auth.ts`, `route.tsx`, `api.ts` (too generic)

- **Python:** snake_case with descriptive names
  - ✓ `market_data_fetcher.py`, `tick_normalizer.py`, `postgres_writer.py`
  - ✗ `fetcher.py`, `normalizer.py`, `writer.py` (too generic)

- **C# / .NET:** PascalCase (language convention)
  - ✓ `AuthService.cs`, `TokenService.cs`, `ApplicationUser.cs`
  - ✗ `authService.cs`, `auth_service.cs` (wrong case)

- **Markdown:** kebab-case, descriptive
  - ✓ `system-architecture.md`, `code-standards.md`
  - ✗ `arch.md`, `standards.md` (too short)

---

## .NET Backend Standards

### Architecture: Clean Architecture (Mandatory)

**4-Layer Structure:**

```
VnStock.API/           ← Controllers, Middleware, Configuration
VnStock.Application/   ← Services, DTOs, Business Logic
VnStock.Infrastructure/ ← EF Core, Database, External Services
VnStock.Domain/        ← Entities, Interfaces, Core Rules
```

**Dependency Flow:** Domain ← Application ← Infrastructure ← API (unidirectional)

**Rules:**
- Domain layer never references other layers (no dependencies)
- Application layer can reference Domain only
- Infrastructure layer can reference Application + Domain
- API layer can reference all (but prefer Application)

### Naming Conventions

| Item | Convention | Example |
|------|-----------|---------|
| **Namespace** | PascalCase, hierarchical | `VnStock.Application.Auth.Services` |
| **Class** | PascalCase (noun) | `AuthService`, `TokenService` |
| **Interface** | PascalCase with `I` prefix | `IAuthService`, `ITokenService` |
| **Property** | PascalCase | `Email`, `PasswordHash`, `ExpiresAt` |
| **Parameter** | camelCase | `email`, `password`, `expiresAt` |
| **Local Variable** | camelCase | `isValid`, `userCount`, `hashResult` |
| **Constant** | UPPER_CASE (uppercase with underscores) | `DEFAULT_TOKEN_LIFETIME` |
| **Private Field** | _camelCase (underscore prefix) | `_tokenService`, `_dbContext` |
| **Async Method** | Verb + "Async" suffix | `RegisterAsync()`, `LoginAsync()` |

### Entity Design

**Example: ApplicationUser Entity**

```csharp
public class ApplicationUser : IdentityUser<Guid>
{
    // No business logic in entities (anemic model is OK)
    // Just properties with validation attributes

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public override string Email { get; set; }

    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<WatchlistItem> Watchlists { get; set; } = new List<WatchlistItem>();
    public ICollection<Portfolio> Portfolios { get; set; } = new List<Portfolio>();
}
```

**Rules:**
- Entities are data containers only (no methods with business logic)
- Use navigation properties for relationships
- Apply validation attributes ([Required], [MaxLength], [EmailAddress], etc.)
- Use `= new List<T>()` for collections (prevents null reference errors)

### Service Design

**Example: AuthService**

```csharp
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task LogoutAsync(Guid userId);
}

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public AuthService(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Validate input
        if (request?.Email == null) throw new ArgumentNullException(nameof(request));

        // Check if user exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return new AuthResponse { Success = false, Message = "Email already registered" };

        // Create new user
        var user = new ApplicationUser { Email = request.Email, UserName = request.Email };
        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return new AuthResponse { Success = false, Message = "Registration failed" };

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user);

        return new AuthResponse { Success = true, AccessToken = accessToken, RefreshToken = refreshToken };
    }

    // ... other methods
}
```

**Rules:**
- Always use interfaces (IAuthService) for dependency injection
- Inject dependencies via constructor
- Methods are public and async (suffixed with "Async")
- Throw appropriate exceptions for error conditions
- Use `?.` operator for null-safe property access
- Keep methods focused (single responsibility)

### Controller Design

**Example: AuthController**

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        if (!response.Success)
            return BadRequest(new ProblemDetails { Title = "Registration failed", Detail = response.Message });

        // Set HttpOnly refresh token cookie
        Response.Cookies.Append("refreshToken", response.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

        return Ok(new { response.AccessToken, response.ExpiresIn, response.TokenType });
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var id))
            return Unauthorized();

        // Retrieve user from database
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return Unauthorized();

        return Ok(new { user.Id, user.Email, user.UserName });
    }
}
```

**Rules:**
- Controllers are thin (5-10 lines per method)
- Call services from controllers; never database directly
- Use [Authorize] attribute for protected endpoints
- Specify [ProducesResponseType] for API documentation
- Return appropriate status codes (200, 201, 400, 401, 404, 500)
- Use ProblemDetails (RFC 7807) for error responses

### Dependency Injection

**Example: Program.cs**

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Services (dependency injection)
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddScoped<UserManager<ApplicationUser>>();

        // Database
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Identity
        builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // Authentication
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => { /* JWT config */ });

        // CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("DevelopmentCors", policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        // Build and run
        var app = builder.Build();
        app.UseRouting();
        app.UseCors("DevelopmentCors");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
```

**Rules:**
- Register all services in Program.cs in order: domain → application → infrastructure → middleware
- Use appropriate lifetimes: Singleton (config), Scoped (DbContext, services), Transient (stateless)
- Configure CORS for development/production separately

### Error Handling

**Pattern: Try-Catch with Logging**

```csharp
public async Task<AuthResponse> LoginAsync(LoginRequest request)
{
    try
    {
        if (request?.Email == null)
            throw new ArgumentNullException(nameof(request), "Login request cannot be null");

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return new AuthResponse { Success = false, Message = "Invalid credentials" };

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
            return new AuthResponse { Success = false, Message = "Invalid credentials" };

        var accessToken = _tokenService.GenerateAccessToken(user);
        return new AuthResponse { Success = true, AccessToken = accessToken };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Login failed for email {Email}", request?.Email);
        throw;
    }
}
```

**Rules:**
- Don't catch exceptions silently (always log or re-throw)
- Use specific exception types (ArgumentNullException, InvalidOperationException)
- Return error messages for validation, throw for unexpected errors
- Log with structured parameters: `{Email}` instead of string concatenation

### Code Comments

**Pattern: Why, not What**

```csharp
// ✓ GOOD: Explains the why
// We use exponential backoff to avoid overwhelming the Redis server during
// connection failures. This prevents cascading failures in the system.
private const int MaxRetryAttempts = 5;
private static readonly TimeSpan BaseRetryDelay = TimeSpan.FromSeconds(1);

// ✗ BAD: States the obvious
// Set the retry count to 5
private const int MaxRetryAttempts = 5;
```

**Rules:**
- Write comments for complex algorithms, not obvious code
- Explain business logic, non-obvious constraints
- Keep comments up-to-date with code changes
- Use TODO comments for known issues (search for them before release)

---

## Python Data Service Standards

### Code Style: PEP 8 with Black Formatter

**Line Length:** 88 characters (Black default)

**Example:**

```python
# ✓ GOOD: Formatted with Black
from typing import Optional, List
from dataclasses import dataclass
from datetime import datetime


@dataclass
class TickData:
    """Normalized tick data from TCBS market feed."""
    symbol: str
    price: float
    volume: int
    timestamp: datetime
    change_percent: Optional[float] = None


async def normalize_tick(raw_tick: dict) -> TickData:
    """
    Normalize raw TCBS tick to standardized format.

    Args:
        raw_tick: Raw tick dictionary from vnstock library

    Returns:
        TickData with normalized fields

    Raises:
        ValueError: If required fields missing or invalid
    """
    if not isinstance(raw_tick, dict):
        raise ValueError("raw_tick must be a dictionary")

    required_fields = ["symbol", "price", "volume", "timestamp"]
    if not all(field in raw_tick for field in required_fields):
        raise ValueError(f"Missing required fields: {required_fields}")

    return TickData(
        symbol=str(raw_tick["symbol"]).upper(),
        price=float(raw_tick["price"]),
        volume=int(raw_tick["volume"]),
        timestamp=datetime.fromisoformat(raw_tick["timestamp"]),
        change_percent=float(raw_tick.get("change_percent", 0.0))
    )
```

**Formatting:**
- Run `black` before commit: `black services/vnstock-service/`
- Use `isort` for import ordering
- Type hints on all functions: `def func(x: int) -> str:`

### Naming Conventions

| Item | Convention | Example |
|------|-----------|---------|
| **Module** | snake_case | `market_data_fetcher.py` |
| **Class** | PascalCase | `TickData`, `MarketDataFetcher` |
| **Function** | snake_case | `normalize_tick()`, `fetch_market_data()` |
| **Variable** | snake_case | `raw_tick`, `connection_string` |
| **Constant** | UPPER_CASE | `DEFAULT_POLL_INTERVAL`, `MAX_RETRIES` |
| **Private Function** | _snake_case (leading underscore) | `_exponential_backoff()` |

### Async/Await Pattern

**Example: Market Data Fetcher**

```python
import asyncio
from typing import AsyncIterator
import aioredis
import asyncpg


class MarketDataFetcher:
    """Fetch real-time market data from TCBS via vnstock."""

    def __init__(self, redis_url: str, postgres_dsn: str):
        self.redis_url = redis_url
        self.postgres_dsn = postgres_dsn
        self.running = False

    async def start(self) -> None:
        """Start continuous market data polling."""
        self.running = True
        tasks = [
            self._polling_loop(),
            self._batch_writer_loop()
        ]
        await asyncio.gather(*tasks)

    async def _polling_loop(self) -> None:
        """Poll TCBS every 3 seconds."""
        retry_count = 0
        while self.running:
            try:
                ticks = await self._fetch_ticks()
                for tick in ticks:
                    await self._queue.put(tick)
                retry_count = 0  # Reset on success
            except Exception as e:
                retry_count = min(retry_count + 1, 5)
                delay = 2 ** retry_count  # Exponential backoff
                await asyncio.sleep(delay)
            else:
                await asyncio.sleep(3)  # Poll interval

    async def _batch_writer_loop(self) -> None:
        """Flush accumulated ticks to PostgreSQL every 5 seconds."""
        batch = []
        while self.running:
            try:
                # Accumulate ticks with timeout
                while len(batch) < 100:
                    try:
                        tick = self._queue.get_nowait()
                        batch.append(tick)
                    except asyncio.QueueEmpty:
                        await asyncio.sleep(0.1)
                        if len(batch) > 0 and (datetime.now() - batch[0]["timestamp"]).seconds > 5:
                            break

                # Flush to database
                if batch:
                    async with asyncpg.create_pool(self.postgres_dsn) as pool:
                        async with pool.acquire() as conn:
                            await conn.executemany(
                                "INSERT INTO ticks (symbol, timestamp, price, volume) VALUES ($1, $2, $3, $4)",
                                [(t["symbol"], t["timestamp"], t["price"], t["volume"]) for t in batch]
                            )
                    batch = []
            except Exception as e:
                await asyncio.sleep(5)
```

**Rules:**
- Use `async def` for functions with I/O (database, network, file)
- Use `await` when calling async functions
- Use `asyncio.gather()` to run multiple tasks concurrently
- Use `asyncio.sleep()` instead of `time.sleep()` (non-blocking)
- Handle timeouts with `asyncio.wait_for()`

### Error Handling

**Pattern: Graceful Degradation**

```python
import logging
from typing import Optional

logger = logging.getLogger(__name__)


async def publish_to_redis(redis_client, channel: str, message: dict) -> bool:
    """
    Publish tick to Redis with error handling.

    Returns True if successful, False if failed (but continues anyway).
    """
    try:
        await redis_client.publish(channel, json.dumps(message))
        return True
    except Exception as e:
        logger.error(
            "Failed to publish to Redis channel %s: %s",
            channel,
            str(e),
            exc_info=True  # Include full stack trace
        )
        return False  # Continue polling even if Redis down


async def write_to_postgres(conn, batch: List[dict]) -> bool:
    """
    Write batch to PostgreSQL with error handling.

    Logs error but doesn't stop polling if database is down.
    """
    try:
        await conn.executemany(
            "INSERT INTO ticks (...) VALUES (...)",
            batch
        )
        return True
    except asyncpg.PostgresError as e:
        logger.error("Database error (will retry): %s", str(e))
        return False
    except Exception as e:
        logger.critical("Unexpected error in database write: %s", str(e))
        return False
```

**Rules:**
- Log at appropriate levels: DEBUG (polling details), INFO (major events), ERROR (failures), CRITICAL (can't recover)
- Return success/failure status, don't raise (allows graceful degradation)
- Always include `exc_info=True` in exception logs
- Don't suppress exceptions silently

### Testing Pattern

**Example: test_tick_normalizer.py**

```python
import pytest
from datetime import datetime
from tick_normalizer import normalize_tick, TickData


class TestTickNormalizer:
    """Test tick data normalization from TCBS format."""

    def test_normalize_valid_tick(self):
        """Should normalize valid TCBS tick data."""
        raw_tick = {
            "symbol": "vcb",
            "price": "180.5",
            "volume": 5000000,
            "timestamp": "2026-03-09T10:30:45.123Z",
            "change_percent": "1.25"
        }

        result = normalize_tick(raw_tick)

        assert result.symbol == "VCB"
        assert result.price == 180.5
        assert result.volume == 5000000
        assert isinstance(result.timestamp, datetime)
        assert result.change_percent == 1.25

    def test_normalize_missing_required_field(self):
        """Should raise ValueError if required field missing."""
        raw_tick = {
            "symbol": "vcb",
            "price": "180.5",
            # missing volume
            "timestamp": "2026-03-09T10:30:45.123Z"
        }

        with pytest.raises(ValueError, match="Missing required fields"):
            normalize_tick(raw_tick)

    def test_normalize_invalid_price(self):
        """Should raise ValueError if price is not numeric."""
        raw_tick = {
            "symbol": "vcb",
            "price": "invalid",
            "volume": 5000000,
            "timestamp": "2026-03-09T10:30:45.123Z"
        }

        with pytest.raises(ValueError):
            normalize_tick(raw_tick)
```

**Rules:**
- One test per behavior (not one test per function)
- Use descriptive test names: `test_<function>_<scenario>`
- Use `pytest.raises()` for exception testing
- Use fixtures for common setup
- Aim for 80%+ code coverage

---

## React Frontend Standards

### Component Structure: Functional Components Only

**Pattern: React Hook Components**

```typescript
import { FC, ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/auth-store';

interface LoginPageProps {
  // Props interface (prefer typed props)
}

/**
 * Login page component.
 *
 * Handles user authentication flow:
 * 1. User enters email/password
 * 2. Submit to /api/auth/login
 * 3. Store JWT in Zustand store
 * 4. Redirect to dashboard
 */
const LoginPage: FC<LoginPageProps> = () => {
  const navigate = useNavigate();
  const { login, error, isLoading } = useAuthStore();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      await login(email, password);
      navigate('/dashboard');
    } catch (err) {
      // Error already set in store
      console.error('Login failed:', err);
    }
  };

  return (
    <div className="login-container">
      <h1>Login</h1>
      <form onSubmit={handleSubmit}>
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="Email"
          required
        />
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          placeholder="Password"
          required
        />
        <button type="submit" disabled={isLoading}>
          {isLoading ? 'Logging in...' : 'Login'}
        </button>
      </form>
      {error && <p className="error">{error}</p>}
    </div>
  );
};

export default LoginPage;
```

**Rules:**
- Components are functions (not class components)
- Use `FC<Props>` type annotation
- All props should be typed in interface
- Use hooks for state management (useState, useEffect, useContext)
- Event handlers are arrow functions (bind `this` implicitly)
- JSDoc comments explain component purpose and data flow

### State Management: Zustand

**Pattern: Auth Store**

```typescript
import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import * as api from '@/services/api-client';

interface AuthState {
  // State
  email: string | null;
  token: string | null;
  isAuthenticated: boolean;
  error: string | null;
  isLoading: boolean;

  // Actions
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  logout: () => void;
  clearError: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      email: null,
      token: null,
      isAuthenticated: false,
      error: null,
      isLoading: false,

      login: async (email: string, password: string) => {
        set({ isLoading: true, error: null });
        try {
          const response = await api.login(email, password);
          set({
            email,
            token: response.accessToken,
            isAuthenticated: true,
            isLoading: false
          });
        } catch (err) {
          set({
            error: 'Invalid credentials',
            isLoading: false
          });
          throw err;
        }
      },

      register: async (email: string, password: string) => {
        set({ isLoading: true, error: null });
        try {
          const response = await api.register(email, password);
          set({
            email,
            token: response.accessToken,
            isAuthenticated: true,
            isLoading: false
          });
        } catch (err) {
          set({
            error: 'Registration failed',
            isLoading: false
          });
          throw err;
        }
      },

      logout: () => {
        set({
          email: null,
          token: null,
          isAuthenticated: false,
          error: null
        });
      },

      clearError: () => set({ error: null })
    }),
    {
      name: 'auth-store', // localStorage key
      partialize: (state) => ({
        email: state.email,
        token: state.token,
        isAuthenticated: state.isAuthenticated
      }) // Persist only these fields
    }
  )
);
```

**Rules:**
- One store per domain (auth, market, user)
- All state mutations through actions
- Use `persist` middleware to survive page refresh
- Type all state with interfaces
- Actions are async (API calls)

### Styling: TailwindCSS Utility Classes

**Pattern: Component Styling**

```typescript
interface ButtonProps {
  variant?: 'primary' | 'secondary' | 'danger';
  disabled?: boolean;
  children: ReactNode;
  onClick?: () => void;
}

const Button: FC<ButtonProps> = ({
  variant = 'primary',
  disabled = false,
  children,
  onClick
}) => {
  const baseClasses = 'px-4 py-2 rounded font-semibold transition';

  const variantClasses = {
    primary: 'bg-blue-600 text-white hover:bg-blue-700',
    secondary: 'bg-gray-200 text-gray-800 hover:bg-gray-300',
    danger: 'bg-red-600 text-white hover:bg-red-700'
  };

  const disabledClasses = disabled ? 'opacity-50 cursor-not-allowed' : '';

  return (
    <button
      className={`${baseClasses} ${variantClasses[variant]} ${disabledClasses}`}
      disabled={disabled}
      onClick={onClick}
    >
      {children}
    </button>
  );
};
```

**Rules:**
- Use Tailwind utility classes, not custom CSS
- Extract repeated class combinations into helper constants
- Use responsive variants: `md:`, `lg:`, `sm:`
- Avoid inline styles (use Tailwind instead)
- Dark mode: use `dark:` prefix

### API Client: Axios with JWT Interceptor

**Pattern: Authenticated Requests**

```typescript
import axios, { AxiosInstance, AxiosError, InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '@/stores/auth-store';

const createApiClient = (): AxiosInstance => {
  const client = axios.create({
    baseURL: 'http://localhost/api',
    headers: {
      'Content-Type': 'application/json'
    },
    withCredentials: true // Include cookies (refreshToken)
  });

  // Request interceptor: Add JWT to headers
  client.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
      const { token } = useAuthStore.getState();
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    },
    (error: AxiosError) => Promise.reject(error)
  );

  // Response interceptor: Handle 401, refresh token
  client.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
      const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

      if (error.response?.status === 401 && !originalRequest._retry) {
        originalRequest._retry = true;

        try {
          // Request new token
          const response = await axios.post('http://localhost/api/auth/refresh', {});
          const { accessToken } = response.data;

          // Update store
          useAuthStore.setState({ token: accessToken });

          // Retry original request with new token
          originalRequest.headers.Authorization = `Bearer ${accessToken}`;
          return client(originalRequest);
        } catch (refreshError) {
          // Refresh failed, logout
          useAuthStore.getState().logout();
          window.location.href = '/login';
          return Promise.reject(refreshError);
        }
      }

      return Promise.reject(error);
    }
  );

  return client;
};

export const apiClient = createApiClient();

// API methods
export const login = (email: string, password: string) =>
  apiClient.post('/auth/login', { email, password });

export const register = (email: string, password: string) =>
  apiClient.post('/auth/register', { email, password, confirmPassword: password });

export const getMe = () =>
  apiClient.get('/auth/me');
```

**Rules:**
- Centralize HTTP client configuration
- Use request interceptor to add auth headers
- Use response interceptor to handle token refresh
- Include credentials (cookies) in requests
- Wrap API calls in try-catch in components

### Routing: Protected Routes

**Pattern: Route Guard**

```typescript
import { Navigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/auth-store';

interface ProtectedRouteProps {
  children: ReactNode;
  requiredRole?: string;
}

export const ProtectedRoute: FC<ProtectedRouteProps> = ({ children, requiredRole }) => {
  const { isAuthenticated } = useAuthStore();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  // TODO: Add role-based access control (Phase 3)
  // if (requiredRole && !userHasRole(requiredRole)) {
  //   return <Navigate to="/unauthorized" replace />;
  // }

  return <>{children}</>;
};

// App routing
import { BrowserRouter, Routes, Route } from 'react-router-dom';

export const App: FC = () => {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route
          path="/dashboard"
          element={
            <ProtectedRoute>
              <DashboardPage />
            </ProtectedRoute>
          }
        />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </BrowserRouter>
  );
};
```

**Rules:**
- Check `isAuthenticated` before rendering protected content
- Redirect to `/login` if not authenticated
- Use React Router's `<Navigate>` component
- Implement role-based access control in Phase 3

### Naming Conventions

| Item | Convention | Example |
|------|-----------|---------|
| **File** | kebab-case with component type suffix | `auth-store.ts`, `login-page.tsx`, `api-client.ts` |
| **Component** | PascalCase (noun) | `LoginPage`, `ProtectedRoute`, `Button` |
| **Interface** | PascalCase with suffix | `LoginPageProps`, `AuthState`, `ButtonProps` |
| **Function** | camelCase | `handleSubmit`, `fetchUser`, `calculateTotal` |
| **Variable** | camelCase | `isLoading`, `userData`, `errorMessage` |
| **Constant** | UPPER_CASE | `DEFAULT_API_TIMEOUT`, `MAX_RETRIES` |

### Code Comments

**Pattern: Why, not What**

```typescript
// ✓ GOOD: Explains business logic
// We persist the auth state to localStorage so users don't have to log in
// after each page refresh. However, we only persist the token, not the
// user ID, to keep the store minimal.
const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({ /* ... */ }),
    {
      name: 'auth-store',
      partialize: (state) => ({ token: state.token })
    }
  )
);

// ✗ BAD: States the obvious
// Create a new Zustand store
const useAuthStore = create<AuthState>(...)
```

**Rules:**
- Comment on complex logic and business decisions
- Explain why, not what (code shows what)
- Keep comments short (one line preferred)
- Update comments when code changes

---

## Database Standards

### Schema Design

**Rules:**
- Use SERIAL or UUID for primary keys (Guid for .NET, UUID for Python)
- Foreign keys with ON DELETE CASCADE (unless data must be preserved)
- NOT NULL constraints on required columns
- UNIQUE constraints on natural identifiers (email, symbol+date)
- CHECK constraints for domain-specific rules (exchange IN ('HOSE', 'HNX', 'UPCOM'))

**Example: Watchlist Table**

```sql
CREATE TABLE watchlists (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES asp_net_users(id) ON DELETE CASCADE,
    symbol VARCHAR(10) NOT NULL REFERENCES stocks(symbol),
    added_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    -- Prevent duplicates (user can't add same symbol twice)
    UNIQUE(user_id, symbol)
);

CREATE INDEX idx_watchlists_user_id ON watchlists(user_id);
```

### Query Optimization

**Indexing Strategy:**

| Query Pattern | Index Type | Example |
|--------------|-----------|---------|
| **Filter + Sort** | B-tree | `(symbol, timestamp DESC)` |
| **Point lookup** | B-tree | `(user_id)` |
| **Active records** | Partial | `(user_id, is_active)` WHERE is_active = true |
| **Full-text search** | GiST/GIN | `(name) USING GIN` |

**Rules:**
- Index columns in WHERE, ORDER BY, JOIN clauses
- Index low-cardinality columns only if queried frequently
- Avoid redundant indexes (if (a,b) indexed, don't index (a) separately)
- Monitor query performance with EXPLAIN ANALYZE

---

## Security Standards

### Authentication

**Rules:**
- Passwords hashed with bcrypt (never plain text)
- JWT tokens signed with HMAC-SHA256
- Refresh tokens invalidated on logout
- HttpOnly cookies prevent JavaScript theft
- SameSite=Strict prevents CSRF attacks
- Rate limit login attempts (5 per minute)

### Authorization

**Rules:**
- Check permissions on server, never trust client
- Use [Authorize] attribute on protected endpoints
- Implement role-based access control (Phase 3)
- Default deny (whitelist allowed users)

### Data Protection

**Rules:**
- Never log passwords or tokens
- Store secrets in environment variables, not source code
- Use parameterized SQL queries (EF Core does this)
- Validate input server-side (don't trust client)
- Encrypt sensitive data at rest (Phase 4)

### API Security

**Rules:**
- HTTPS only in production (TLS 1.2+)
- CORS configured to trusted origins only
- Rate limiting on all endpoints
- Input validation (email format, password strength)
- Output encoding (prevent XSS)

---

## Testing Standards

### Unit Tests

**Coverage Targets:**
- 80%+ code coverage for all services
- 100% coverage for authentication/authorization
- 100% coverage for data transformation
- No mocking unless necessary (test real behavior)

### Test Organization

**Pattern:**
```
tests/
├── unit/
│   ├── auth-store.test.ts
│   ├── api-client.test.ts
│   └── tick-normalizer.test.py
├── integration/
│   ├── auth-flow.test.ts
│   └── data-service.test.py
└── e2e/
    └── (Phase 4)
```

### Running Tests

**Frontend:**
```bash
cd client
npm test                    # Run all tests
npm test -- --coverage      # With coverage report
npm test -- --watch         # Watch mode
```

**Backend:**
```bash
cd src
dotnet test                 # Run all tests
dotnet test /p:CollectCoverage=true  # With coverage
```

**Data Service:**
```bash
cd services/vnstock-service
pytest                      # Run all tests
pytest --cov=.              # With coverage
pytest -v                   # Verbose output
```

---

## Git & Commit Standards

### Branch Naming

**Convention:** `{type}/{brief-description}`

| Type | Purpose | Example |
|------|---------|---------|
| `feat/` | New feature | `feat/phase02-signalr-hub` |
| `fix/` | Bug fix | `fix/token-refresh-timeout` |
| `docs/` | Documentation | `docs/update-architecture` |
| `refactor/` | Code refactoring | `refactor/extract-auth-service` |
| `test/` | Tests only | `test/add-auth-service-tests` |
| `chore/` | Dependencies, config | `chore/upgrade-dotnet-8.0.3` |

### Commit Message Format

**Convention:** [Conventional Commits](https://www.conventionalcommits.org/)

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Example:**

```
feat(auth): implement JWT refresh token rotation

- Access token: 15 minutes (memory)
- Refresh token: 7 days (HttpOnly cookie)
- On refresh: invalidate old token (rotation)
- Prevents token replay attacks

Closes #42
```

**Rules:**
- Type: feat, fix, docs, refactor, test, chore
- Scope: area of code (auth, market, portfolio)
- Subject: imperative mood, no period, <50 chars
- Body: explain what and why, not how
- Footer: reference issues (#123)

### Code Review Checklist

Before pushing:
- [ ] Code follows standards in this file
- [ ] All tests pass (no ignored tests)
- [ ] No console.log, TODO comments, or debug code
- [ ] No hardcoded secrets (check .env.example for required vars)
- [ ] Commit message follows conventional format
- [ ] Branch is up-to-date with main

---

## Linting & Formatting

### .NET Backend

**Analyzer:** StyleCop via `.editorconfig`

```bash
# Check
dotnet build

# Auto-fix (limited)
# Manual fixes required for most issues
```

### Python Data Service

**Formatter:** Black
**Linter:** Flake8 + Pylint

```bash
# Format
black services/vnstock-service/

# Lint
flake8 services/vnstock-service/
pylint services/vnstock-service/
```

### React Frontend

**Formatter:** Prettier
**Linter:** ESLint

```bash
# Format
npm run format

# Lint
npm run lint

# Lint + fix
npm run lint -- --fix
```

---

## Documentation Standards

### Code Comments

- Explain "why" not "what"
- Keep concise (one line preferred)
- Update comments when code changes
- Use JSDoc/docstrings for public APIs

### Markdown Files

- One topic per file
- Clear hierarchy (H1 → H2 → H3)
- Code blocks with syntax highlighting
- Links to related files/concepts
- Update last modified date

### README per Service

Each service should have minimal README:
- Purpose and quick start
- Dependencies
- Key files
- How to run tests
- Deployment instructions

---

## Continuous Improvement

### When to Refactor

- Code is duplicated (DRY violation)
- Method exceeds 30 lines
- Class has too many responsibilities
- Test names don't clearly describe behavior
- Future feature requires significant rework

### Deprecation Policy

- Mark old API with [Obsolete("reason")] in .NET
- Leave deprecated for one full phase before removal
- Document migration path in comments
- Update changelog with deprecation notice

---

**Last Updated:** 2026-03-09 | **Status:** Phase 1 Complete (v0.1.0-foundation)
