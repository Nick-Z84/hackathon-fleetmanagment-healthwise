using VehicleBookingSystem.Core;
using VehicleBookingSystem.Core.Interfaces;
using VehicleBookingSystem.Core.Models;

namespace VehicleBookingSystem.Infrastructure.Data;

public static class DataSeeder
{
    private static readonly (string Make, string Model, int Year)[] Fleet =
    [
        ("Toyota", "HiLux", 2022), ("Toyota", "LandCruiser", 2021), ("Toyota", "Camry", 2023),
        ("Toyota", "RAV4", 2022),  ("Toyota", "Prado", 2020),       ("Ford", "Ranger", 2023),
        ("Ford", "Everest", 2022), ("Ford", "Transit", 2021),        ("Ford", "F-250", 2022),
        ("Mitsubishi", "Triton", 2023), ("Mitsubishi", "Outlander", 2022), ("Mitsubishi", "Pajero Sport", 2021),
        ("Isuzu", "D-Max", 2022),  ("Isuzu", "MU-X", 2023),         ("Mazda", "BT-50", 2022),
        ("Mazda", "CX-5", 2023),   ("Holden", "Colorado", 2019),    ("Nissan", "Navara", 2022),
        ("Nissan", "Patrol", 2021),("Nissan", "X-Trail", 2023),      ("Volkswagen", "Amarok", 2022),
        ("Volkswagen", "Transporter", 2021), ("Mercedes-Benz", "Sprinter", 2022),
        ("Hyundai", "Santa Fe", 2023), ("Kia", "Sorento", 2022)
    ];

    private static readonly (string First, string Last, string Username, bool HasLicence, StaffRole Role)[] StaffSeed =
    [
        ("Nick",       "Zhou",      "nick.zhou",       true,  StaffRole.Admin),
        ("Sarah",      "Johnson",   "sarah.johnson",   true,  StaffRole.User),
        ("Michael",    "Thompson",  "michael.thompson",true,  StaffRole.User),
        ("Emily",      "Williams",  "emily.williams",  true,  StaffRole.User),
        ("James",      "Brown",     "james.brown",     true,  StaffRole.User),
        ("Jessica",    "Davis",     "jessica.davis",   true,  StaffRole.User),
        ("Daniel",     "Miller",    "daniel.miller",   true,  StaffRole.User),
        ("Olivia",     "Wilson",    "olivia.wilson",   true,  StaffRole.User),
        ("Matthew",    "Moore",     "matthew.moore",   true,  StaffRole.User),
        ("Ashley",     "Taylor",    "ashley.taylor",   true,  StaffRole.User),
        ("Joshua",     "Anderson",  "joshua.anderson", true,  StaffRole.User),
        ("Amanda",     "Thomas",    "amanda.thomas",   false, StaffRole.User),
        ("Christopher","Jackson",   "chris.jackson",   false, StaffRole.User),
        ("Megan",      "White",     "megan.white",     false, StaffRole.User),
        ("Andrew",     "Harris",    "andrew.harris",   false, StaffRole.User),
        ("Lauren",     "Martin",    "lauren.martin",   true,  StaffRole.User),
        ("Ryan",       "Garcia",    "ryan.garcia",     true,  StaffRole.User),
        ("Stephanie",  "Martinez",  "stephanie.m",     false, StaffRole.User),
        ("Tyler",      "Robinson",  "tyler.robinson",  false, StaffRole.User),
        ("Brittany",   "Clark",     "brittany.clark",  false, StaffRole.User)
    ];

    private static readonly string[] Purposes =
    [
        "Client home visit", "Allied health outreach", "Community services delivery",
        "Staff transport", "Supply run", "Equipment transfer", "Site inspection",
        "Training session transport", "Conference travel", "Medical appointment escort"
    ];

    private static readonly string[] Destinations =
    [
        "Armadale Health Service", "Cannington Community Centre",
        "Gosnells Community Hub",  "Mandurah Forum",
        "Midland Health Campus",   "Rockingham General Hospital"
    ];

    private static readonly string[] Stations = ["BP", "Shell", "Caltex", "Ampol", "7-Eleven", "United"];
    private static readonly string[] Allocations =
        ["Operations", "Engineering", "Allied Health", "Executive", "Maintenance", "Community Services"];
    private static readonly string[] InsuranceProviders =
        ["NRMA", "Allianz", "QBE", "Suncorp", "CGU", "IAG"];

    public static async Task SeedAsync(
        IVehicleRepository     vehicleRepo,
        IBookingRepository     bookingRepo,
        IStaffRepository       staffRepo,
        IFuelReceiptRepository fuelRepo,
        IAuditLogRepository    auditRepo,
        IPasswordHasher        hasher)
    {
        var rng   = new Random(42);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var now   = DateTime.Now;

        var validLocations = new HashSet<string>(FleetLocations.All, StringComparer.OrdinalIgnoreCase);

        // ?? Always: purge vehicles whose location is outside the canonical list ??
        var allExisting = (await vehicleRepo.GetAllAsync()).ToList();
        foreach (var bad in allExisting.Where(v => v.Location == null || !validLocations.Contains(v.Location)).ToList())
        {
            var badBookings = (await bookingRepo.GetByVehicleIdAsync(bad.Id)).ToList();
            foreach (var b in badBookings) await bookingRepo.DeleteAsync(b.Id);
            var badFuel = (await fuelRepo.GetByVehicleIdAsync(bad.Id)).ToList();
            foreach (var f in badFuel) await fuelRepo.DeleteAsync(f.Id);
            await vehicleRepo.DeleteAsync(bad.Id);
        }

        // ?? Always: backfill TreadDepth for any vehicle that doesn't have one ??
        var rngTread = new Random();   // unseeded so each app start gives different distribution
        var vehiclesNeedingTread = (await vehicleRepo.GetAllAsync())
            .Where(v => v.TreadDepth == null)
            .ToList();
        foreach (var v in vehiclesNeedingTread)
        {
            v.TreadDepth = (TreadDepth)rngTread.Next(3);
            await vehicleRepo.UpdateAsync(v);
        }

        // ?? Ensure at least one Available vehicle per location ????????????
        var currentVehicles = (await vehicleRepo.GetAllAsync()).ToList();
        foreach (var loc in FleetLocations.All)
        {
            if (currentVehicles.Any(v => v.Location == loc && v.Status == VehicleStatus.Available))
                continue;

            // Add a baseline vehicle for this location
            var (make, model, year) = Fleet[rng.Next(Fleet.Length)];
            var plate = $"{(char)('A'+rng.Next(26))}{(char)('A'+rng.Next(26))}" +
                        $"{rng.Next(10)}{rng.Next(10)}{rng.Next(10)}{(char)('A'+rng.Next(26))}";
            var added = await vehicleRepo.AddAsync(new Vehicle
            {
                Make = make, Model = model, Year = year, LicensePlate = plate,
                Status = VehicleStatus.Available,
                Odometer        = rng.Next(5_000, 40_000),
                InitialOdometer = 0,
                FirstRegisteredDate   = today.AddYears(-(DateTime.Today.Year - year)).AddDays(-rng.Next(0, 90)),
                LastServiceDate       = today.AddDays(-rng.Next(30, 180)),
                NextServiceDate       = today.AddDays(rng.Next(90, 365)),
                Location              = loc,
                RegistrationNumber    = $"REG{rng.Next(100, 999):D3}",
                RegistrationExpiry    = today.AddDays(rng.Next(30, 365)),
                InsuranceProvider     = InsuranceProviders[rng.Next(InsuranceProviders.Length)],
                InsurancePolicyNumber = $"POL{rng.Next(100_000, 999_999)}",
                InsuranceExpiry       = today.AddDays(rng.Next(30, 365)),
                TreadDepth            = (TreadDepth)rng.Next(3)
            });
            currentVehicles.Add(added);
        }

        // Skip full seed if vehicles already existed before purge
        if (allExisting.Any(v => validLocations.Contains(v.Location ?? ""))) return;

        // ?? Vehicles ??????????????????????????????????????????????????????
        var vehicleList = new List<Vehicle>();
        for (int i = 0; i < Fleet.Length; i++)
        {
            var (make, model, year) = Fleet[i];
            var odo             = rng.Next(15_000, 120_000);
            var initialOdo      = rng.Next(0, 5_000);
            var firstRegistered = today.AddYears(-(DateTime.Today.Year - year)).AddDays(-rng.Next(0, 180));
            var lastService     = today.AddDays(-rng.Next(30, 365));
            var nextService     = lastService.AddDays(rng.Next(90, 180));
            var regExpiry       = today.AddDays(rng.Next(-30, 365));
            var insExpiry       = today.AddDays(rng.Next(-30, 365));
            var plate           = $"{(char)('A'+rng.Next(26))}{(char)('A'+rng.Next(26))}{rng.Next(10)}{rng.Next(10)}{rng.Next(10)}{(char)('A'+rng.Next(26))}";

            var vehicle = await vehicleRepo.AddAsync(new Vehicle
            {
                Make = make, Model = model, Year = year, LicensePlate = plate,
                Status = VehicleStatus.Available,
                Odometer = odo, InitialOdometer = initialOdo,
                FirstRegisteredDate   = firstRegistered,
                LastServiceDate       = lastService,
                NextServiceDate       = nextService,
                Location              = FleetLocations.All[i % FleetLocations.All.Length],
                FleetAllocation       = Allocations[rng.Next(Allocations.Length)],
                RegistrationNumber    = $"REG{100 + i:D3}",
                RegistrationExpiry    = regExpiry,
                InsuranceProvider     = InsuranceProviders[rng.Next(InsuranceProviders.Length)],
                InsurancePolicyNumber = $"POL{rng.Next(100_000, 999_999)}",
                InsuranceExpiry       = insExpiry,
                TreadDepth            = (TreadDepth)rng.Next(3)
            });
            vehicleList.Add(vehicle);
        }

        // ?? Staff ?????????????????????????????????????????????????????????
        var staffList = new List<Staff>();
        foreach (var (first, last, username, hasLicence, role) in StaffSeed)
        {
            var (hash, salt) = hasher.Hash("123456");

            DateOnly? expiry = null;
            if (hasLicence)
            {
                var idx = staffList.Count(s => s.HasDriversLicence);
                expiry = idx < 3
                    ? today.AddDays(-rng.Next(1, 90))
                    : today.AddDays(rng.Next(90, 1_095));
            }

            var staff = await staffRepo.AddAsync(new Staff
            {
                FirstName = first, LastName = last,
                Username = username, PasswordHash = hash, PasswordSalt = salt,
                Role = role, HasDriversLicence = hasLicence, DriversLicenceExpiry = expiry
            });
            staffList.Add(staff);
        }

        var licenced = staffList.Where(s => s.IsLicenceCurrentAsOf(today)).ToList();
        var allStaff = staffList;

        // ?? 50 bookings spanning this month ? next month ??????????????????
        var monthStart  = new DateTime(now.Year, now.Month, 1);
        var monthEnd    = monthStart.AddMonths(2).AddDays(-1); // end of next month

        // Track latest-used end time per vehicle to avoid overlaps
        var vehicleFree = vehicleList.ToDictionary(v => v.Id, _ => monthStart);
        int booked = 0;
        var shuffled = vehicleList.OrderBy(_ => rng.Next()).ToList();
        int vi = 0;

        while (booked < 50)
        {
            var vehicle = shuffled[vi % shuffled.Count];
            vi++;

            var earliest = vehicleFree[vehicle.Id];
            var start    = earliest.AddHours(rng.Next(1, 24));
            var duration = rng.Next(2, 48); // hours
            var end      = start.AddHours(duration);

            if (end > monthEnd) continue;

            var driver   = licenced[rng.Next(licenced.Count)];
            var bookedBy = allStaff[rng.Next(allStaff.Count)];
            var isPast   = end < now;
            var km       = rng.Next(20, 400);

            await bookingRepo.AddAsync(new Booking
            {
                VehicleId     = vehicle.Id,
                BookedBy      = bookedBy.FullName,
                DriverStaffId = driver.Id,
                Driver        = driver.FullName,
                Purpose       = Purposes[rng.Next(Purposes.Length)],
                Destination   = Destinations[rng.Next(Destinations.Length)],
                IsBusinessUse = rng.Next(10) > 2,
                StartTime     = start,
                EndTime       = end,
                ActualEndTime = isPast ? end : null,
                OdometerStart = vehicle.Odometer,
                OdometerEnd   = isPast ? vehicle.Odometer + km : null,
                Status        = isPast ? BookingStatus.Completed : BookingStatus.Active
            });

            if (isPast) vehicle.Odometer += km;
            vehicleFree[vehicle.Id] = end;
            booked++;
        }

        // ?? 30 additional future bookings in the next 30 days ????????????
        var futureStart  = now;
        var futureEnd    = now.AddDays(30);
        var futureFree   = vehicleList.ToDictionary(v => v.Id, _ => futureStart);
        int futureBooked = 0;
        var shuffled2    = vehicleList.OrderBy(_ => rng.Next(100)).ToList();
        int vj = 0;

        while (futureBooked < 30)
        {
            var vehicle  = shuffled2[vj % shuffled2.Count];
            vj++;

            var earliest = futureFree[vehicle.Id];
            if (earliest >= futureEnd) continue;

            var start    = earliest.AddHours(rng.Next(2, 18));
            if (start >= futureEnd) continue;

            var duration = rng.Next(2, 24);
            var end      = start.AddHours(duration);
            if (end > futureEnd) end = futureEnd;

            var driver   = licenced[rng.Next(licenced.Count)];
            var bookedBy = allStaff[rng.Next(allStaff.Count)];

            await bookingRepo.AddAsync(new Booking
            {
                VehicleId     = vehicle.Id,
                BookedBy      = bookedBy.FullName,
                DriverStaffId = driver.Id,
                Driver        = driver.FullName,
                Purpose       = Purposes[rng.Next(Purposes.Length)],
                Destination   = Destinations[rng.Next(Destinations.Length)],
                IsBusinessUse = rng.Next(10) > 3,
                StartTime     = start,
                EndTime       = end,
                ActualEndTime = null,
                OdometerStart = vehicle.Odometer,
                OdometerEnd   = null,
                Status        = BookingStatus.Active
            });

            futureFree[vehicle.Id] = end;
            futureBooked++;
        }

        // ?? 100 fuel receipts ?????????????????????????????????????????????
        for (int i = 0; i < 100; i++)
        {
            var vehicle  = vehicleList[rng.Next(vehicleList.Count)];
            var daysBack = rng.Next(1, 180);
            var date     = today.AddDays(-daysBack);
            var litres   = Math.Round((decimal)(rng.NextDouble() * 60 + 20), 2);
            var cpl      = Math.Round((decimal)(rng.NextDouble() * 0.50 + 1.70), 4);
            var uploader = licenced[rng.Next(licenced.Count)];

            await fuelRepo.AddAsync(new FuelReceipt
            {
                VehicleId       = vehicle.Id,
                Date            = date,
                Litres          = litres,
                CostPerLitre    = cpl,
                TotalCost       = Math.Round(litres * cpl, 2),
                Station         = Stations[rng.Next(Stations.Length)],
                OdometerReading = vehicle.Odometer - rng.Next(100, 5_000),
                UploadedBy      = uploader.FullName,
                UploadedAt      = DateTime.UtcNow.AddDays(-daysBack)
            });
        }
    }
}
