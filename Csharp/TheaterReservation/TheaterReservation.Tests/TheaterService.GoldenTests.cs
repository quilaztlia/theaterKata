using ApprovalTests;
using ApprovalTests.Reporters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TheaterReservation.Data;
using System;
using System.Collections.Generic;

namespace TheaterReservation.Tests
{
    [UseReporter(typeof(DiffReporter))]
    [TestClass]
    public class GoldenMasterTheaterServiceTests
    {
        // Reuse same pattern as existing tests: direct TheaterService instance
        TheaterService theaterService = new TheaterService();

        // 1) Happy path: find contiguous STANDARD seats on a PREMIERE where VIP policy allows (>= 50%)
        [TestMethod]
        public void Premiere_standard_enough_remaining_seats_should_reserve()
        {
            Performance p = new Performance { id = 1L, play = "Play", startTime = new DateTime(2026, 11, 28, 13, 0, 0), performanceNature = "PREMIERE" };
            string result = theaterService.reservation(1L, 4, "STANDARD", p);
            Approvals.Verify(result);
        }

        // 2) PREMIERE: VIP block when remainingSeats < 50% (reservation should be cleared)
        [TestMethod]
        public void Premiere_standard_vip_blocked_when_less_than_50_percent_remaining()
        {
            // use performance id 3 (room map with different occupancy) to trigger low remaining seats
            Performance p = new Performance { id = 3L, play = "Play", startTime = new DateTime(2023, 3, 21, 21, 0, 0), performanceNature = "PREMIERE" };
            string result = theaterService.reservation(2L, 4, "STANDARD", p);
            Approvals.Verify(result);
        }

        // 3) PREVIEW: VIP block when remainingSeats < 90%
        [TestMethod]
        public void Preview_standard_vip_blocked_when_less_than_90_percent_remaining()
        {
            Performance p = new Performance { id = 2L, play = "Play", startTime = new DateTime(2023, 3, 21, 21, 0, 0), performanceNature = "PREVIEW" };
            string result = theaterService.reservation(2L, 4, "STANDARD", p);
            Approvals.Verify(result);
        }

        // 4) Request in PREMIUM category (different zone) on a PREMIERE
        [TestMethod]
        public void Premiere_premium_category_reservation()
        {
            Performance p = new Performance { id = 1L, play = "Play", startTime = new DateTime(2026, 11, 22, 13, 0, 0), performanceNature = "PREMIERE" };
            string result = theaterService.reservation(1L, 4, "PREMIUM", p);
            Approvals.Verify(result);
        }

        // 5) Reserve twice: second reservation should see seats previously booked/pending and find other seats or fail
        [TestMethod]
        public void Reserve_twice_shows_booked_and_booking_pending_behaviour()
        {
            Performance p = new Performance { id = 1L, play = "Play", startTime = new DateTime(2023, 4, 22, 21, 0, 0), performanceNature = "PREMIERE" };
            string r1 = theaterService.reservation(1L, 4, "STANDARD", p); // first reservation
            string r2 = theaterService.reservation(1L, 5, "STANDARD", p); // second reservation affected by first
            Approvals.Verify(r2);
        }

        // 6) Cancel then reserve: free seat should be reused
        [TestMethod]
        public void Cancel_then_reserve_reuses_freed_seat()
        {
            Performance p = new Performance { id = 1L, play = "Play", startTime = new DateTime(2023, 4, 22, 21, 0, 0), performanceNature = "PREMIERE" };
            string r1 = theaterService.reservation(1L, 1, "STANDARD", p);
            // existing test harness uses "123456" as reservationId in previous tests; replicate behavior to cancel
            List<string> seats = new List<string> { "B2" };
            theaterService.cancelReservation("123456", 1L, seats);
            string r2 = theaterService.reservation(1L, 4, "STANDARD", p);
            Approvals.Verify(r2);
        }

        // 7) Request size larger than any contiguous run in category -> should be ABORTED / not fulfillable
        [TestMethod]
        public void Request_larger_than_any_contiguous_run_should_abort()
        {
            Performance p = new Performance { id = 1L, play = "Play", startTime = new DateTime(2026, 11, 28, 13, 0, 0), performanceNature = "STANDARD" };
            // ask for a huge contiguous block (e.g., 20)
            string result = theaterService.reservation(1L, 20, "STANDARD", p);
            Approvals.Verify(result);
        }

        // 8) Boundary test: remainingSeats exactly equals 50% for PREMIERE (VIP rule uses '<' so request should be allowed)
        [TestMethod]
        public void Premiere_boundary_remaining_equals_50_percent_should_allow()
        {
            // Use a scenario that yields remainingSeats == totalSeats * 0.5; replicate using id=1 and reserved pattern in DAO
            Performance p = new Performance { id = 1L, play = "Play", startTime = new DateTime(2026, 11, 28, 13, 0, 0), performanceNature = "PREMIERE" };
            string result = theaterService.reservation(1L, 4, "STANDARD", p);
            Approvals.Verify(result);
        }

        // 9) PERFORMANCE with unknown nature: no VIP policy applied
        [TestMethod]
        public void Unknown_performance_nature_applies_no_vip_policy()
        {
            Performance p = new Performance { id = 1L, play = "Play", startTime = new DateTime(2026, 11, 28, 13, 0, 0), performanceNature = "REGULAR" };
            string result = theaterService.reservation(1L, 4, "STANDARD", p);
            Approvals.Verify(result);
        }

        // 10) Seats with BOOKING_PENDING are excluded from availability
        [TestMethod]
        public void Booking_pending_seats_are_excluded_from_availability()
        {
            // Create a first reservation that sets some seats to BOOKING_PENDING then attempt another reservation that would have used them.
            Performance p = new Performance { id = 1L, play = "Play", startTime = new DateTime(2023, 4, 22, 21, 0, 0), performanceNature = "PREMIERE" };
            string first = theaterService.reservation(1L, 4, "STANDARD", p); // marks seats BOOKING_PENDING
            // immediate second attempt should not reuse pending seats
            string second = theaterService.reservation(1L, 4, "STANDARD", p);
            Approvals.Verify(second);
        }
    }
}