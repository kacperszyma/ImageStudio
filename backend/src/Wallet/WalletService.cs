using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Wallet.Contracts;
using System.Data;
using Npgsql;

namespace Wallet;

internal sealed class WalletService(WalletDbContext db, IPaymentGateway paymentGateway) : IWalletService
{
    private const string HoldActive = "active";
    private const string HoldReleased = "released";
    private const string HoldCharged = "charged";

    public async Task<long> GetBalanceAsync(Guid userId)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
        return account?.Balance ?? 0;
    }

    public async Task EnsureAccountAsync(Guid userId)
    {
        if (await db.Accounts.AnyAsync(a => a.UserId == userId))
            return;

        db.Accounts.Add(new WalletAccount { UserId = userId, Balance = WalletAccount.StartingBalance });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Account was created concurrently by another request — nothing to do.
        }
    }

    public async Task FreezeFundsAsync(Guid userId, long amount, Guid generationJobId, string idempotencyKey)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);

        if (await HoldExistsAsync(idempotencyKey)) return;

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var wallet = await LockWalletAsync(userId);
            if (await HoldExistsAsync(idempotencyKey)) return;   // concurrent duplicate

            if (wallet.Balance < amount)
                throw new InsufficientFundsException(userId, amount, wallet.Balance);

            wallet.Balance -= amount;
            wallet.Frozen += amount;

            db.Ledger.Add(new WalletLedger
            {
                CreatedAt = DateTime.UtcNow,
                WalletId = userId,
                Amount = amount,                 // magnitude; direction is conveyed by Type
                Type = TransactionType.Freeze,
                BalanceAfter = wallet.Balance,
                IdempotencyKey = idempotencyKey,
            });
            db.Holds.Add(new WalletHold
            {
                CreatedAt = DateTime.UtcNow,
                WalletId = userId,
                PurchaseId = generationJobId,
                Amount = amount,
                Status = HoldActive,
                IdempotencyKey = idempotencyKey,
            });

            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await tx.RollbackAsync();   // duplicate idempotency key — already processed
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task UnfreezeAsync(Guid generationJobId)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var hold = await db.Holds.FirstOrDefaultAsync(
                h => h.PurchaseId == generationJobId && h.Status == HoldActive);
            if (hold is null)
                return;   // no active hold — never frozen or already finalized; idempotent no-op

            var wallet = await LockWalletAsync(hold.WalletId);

            wallet.Balance += hold.Amount;
            wallet.Frozen -= hold.Amount;

            db.Ledger.Add(new WalletLedger
            {
                CreatedAt = DateTime.UtcNow,
                WalletId = wallet.UserId,
                Amount = hold.Amount,            // refund back to spendable balance
                Type = TransactionType.Unfreeze,
                BalanceAfter = wallet.Balance,
                IdempotencyKey = $"unfreeze_{generationJobId}",
            });
            hold.Status = HoldReleased;
            hold.ReleasedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await tx.RollbackAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task ChargeFrozenAsync(Guid generationJobId)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var hold = await db.Holds.FirstOrDefaultAsync(
                h => h.PurchaseId == generationJobId && h.Status == HoldActive);
            if (hold is null)
                return;   // already finalized; idempotent no-op

            var wallet = await LockWalletAsync(hold.WalletId);

            // Balance was already debited when the hold was created — charging just
            // consumes the reservation and never returns to spendable balance.
            wallet.Frozen -= hold.Amount;

            db.Ledger.Add(new WalletLedger
            {
                CreatedAt = DateTime.UtcNow,
                WalletId = wallet.UserId,
                Amount = hold.Amount,
                Type = TransactionType.Charge,
                BalanceAfter = wallet.Balance,
                IdempotencyKey = $"charge_{hold.Id}",
            });
            hold.Status = HoldCharged;
            hold.ReleasedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await tx.RollbackAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task TopUpAsync(Guid userId, long amount, string idempotencyKey, Guid? purchaseId = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);

        if (await LedgerEntryExistsAsync(idempotencyKey)) return;

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var wallet = await LockWalletAsync(userId);
            if (await LedgerEntryExistsAsync(idempotencyKey)) return;   // concurrent duplicate

            Credit(wallet, amount, idempotencyKey, purchaseId);

            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await tx.RollbackAsync();   // duplicate webhook delivery — already credited
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // Applies a credit to the wallet's spendable balance and records the matching
    // ledger row. Transaction-agnostic: the caller must already hold an open
    // transaction and the wallet row lock (see TopUpAsync / ProcessPaymentWebhookAsync).
    private void Credit(WalletAccount wallet, long amount, string idempotencyKey, Guid? purchaseId)
    {
        wallet.Balance += amount;
        db.Ledger.Add(new WalletLedger
        {
            CreatedAt = DateTime.UtcNow,
            WalletId = wallet.UserId,
            Amount = amount,
            Type = TransactionType.TopUp,
            BalanceAfter = wallet.Balance,
            IdempotencyKey = idempotencyKey,
            PurchaseId = purchaseId,
        });
    }

    public List<PackageOfferDto> GetPackages() =>
        PebblePackage.All.Select(p => new PackageOfferDto(p.NameId,p.DollarPrice,p.PebbleAmount,p.DisountRate)).ToList();
    

    public async Task<IReadOnlyList<TransactionDto>> GetSpendingHistoryAsync(Guid userId) =>
        await db.Ledger
            .Where(l => l.WalletId == userId && l.Type == TransactionType.Charge)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new TransactionDto(l.Id, l.WalletId, l.Amount, l.Type, l.CreatedAt))
            .ToListAsync();

    public async Task<IReadOnlyList<PurchaseDto>> GetPurchasesAsync(Guid userId) =>
        await db.Purchases
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PurchaseDto(p.Id, p.PackageNameId, p.DollarAmount, p.PebbleAmount, p.CreatedAt))
            .ToListAsync();

    public bool VerifyPaymentWebhook(string payload, string signature) =>
        paymentGateway.VerifyWebhookSignature(payload, signature);

    public Task<string> CreateCheckoutAsync(Guid userId, string packageId) =>
        paymentGateway.CreateCheckoutSessionAsync(userId, packageId);

    public async Task ProcessPaymentWebhookAsync(string payload, string signature)
    {
        if (!paymentGateway.VerifyWebhookSignature(payload, signature))
            throw new WebhookVerificationException("Invalid Stripe webhook signature.");

        var evt = paymentGateway.ParseCheckoutCompleted(payload);
        if (evt is null) return; // unhandled event type — not an error

        var package = PebblePackage.FromName(evt.PackageId);
        var amount = (long)package.PebbleAmount;

        // The ledger idempotency key is the Stripe session id, so a redelivery we've
        // already fully processed is a no-op. Crucially, idempotency is keyed on the
        // CREDIT, not on the purchase row — so a delivery that recorded the purchase
        // but never credited (a crash between the two) still gets healed below.
        if (await LedgerEntryExistsAsync(evt.SessionId)) return;

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var wallet = await LockWalletAsync(evt.UserId);
            if (await LedgerEntryExistsAsync(evt.SessionId)) return; // concurrent duplicate

            // Reuse the purchase from a prior partial delivery if present (keeps the
            // ledger FK valid); otherwise record it now. Purchase and credit commit
            // together — a crash can never again leave one without the other.
            var purchase = await db.Purchases.FirstOrDefaultAsync(p => p.ExternalPaymentId == evt.SessionId);
            if (purchase is null)
            {
                purchase = new WalletPurchase
                {
                    Id = Guid.NewGuid(),
                    UserId = evt.UserId,
                    PackageNameId = evt.PackageId,
                    DollarAmount = package.DollarPrice,
                    PebbleAmount = amount,
                    ExternalPaymentId = evt.SessionId,
                    CreatedAt = DateTime.UtcNow,
                };
                db.Purchases.Add(purchase);
            }

            Credit(wallet, amount, evt.SessionId, purchase.Id);

            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await tx.RollbackAsync(); // duplicate delivery — already recorded and credited
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<TransactionDetailDto?> GetTransactionAsync(Guid transactionId, Guid userId)
    {
        // Scope by userId: a caller may only read their own ledger entries. A
        // non-owner gets the same null as a missing entry, so ids can't be probed.
        var entry = await db.Ledger.FirstOrDefaultAsync(l => l.Id == transactionId && l.WalletId == userId);
        if (entry is null) return null;

        Guid? generationJobId = null;
        if (entry.Type == TransactionType.Charge &&
            entry.IdempotencyKey.StartsWith("charge_") &&
            Guid.TryParse(entry.IdempotencyKey["charge_".Length..], out var holdId))
        {
            var hold = await db.Holds.FirstOrDefaultAsync(h => h.Id == holdId);
            generationJobId = hold?.PurchaseId;
        }

        return new TransactionDetailDto(entry.Id, entry.WalletId, entry.Amount, entry.Type, entry.CreatedAt, generationJobId);
    }

    public async Task<GenerationWalletDetails?> GetGenerationWalletDetailsAsync(Guid jobId)
    {
        var freeze = await db.Ledger.FirstOrDefaultAsync(
            l => l.IdempotencyKey == jobId.ToString() && l.Type == TransactionType.Freeze);
        if (freeze is null) return null;

        return new GenerationWalletDetails(
            BalanceBefore: freeze.BalanceAfter + freeze.Amount,
            BalanceAfter: freeze.BalanceAfter);
    }

    // Locks the wallet row for the duration of the transaction (SELECT ... FOR UPDATE);
    // EF has no first-class API for row locks, so this uses raw SQL.
    private Task<WalletAccount> LockWalletAsync(Guid userId) =>
        db.Accounts
            .FromSqlInterpolated($"SELECT * FROM wallet.accounts WHERE \"UserId\" = {userId} FOR UPDATE")
            .SingleAsync();

    private Task<bool> HoldExistsAsync(string key) =>
        db.Holds.AnyAsync(h => h.IdempotencyKey == key);

    private Task<bool> LedgerEntryExistsAsync(string key) =>
        db.Ledger.AnyAsync(l => l.IdempotencyKey == key);

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: "23505" };
}
