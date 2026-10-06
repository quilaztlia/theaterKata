using ApprovalTests;
using ApprovalTests.Reporters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TheaterReservation.Data;

namespace TheaterReservation.Tests
{
    [UseReporter(typeof(DiffReporter))]
    [TestClass]
    public class TheaterServiceTest
    {
        TheaterService theaterService = new TheaterService();

        [TestMethod]
        public void reserve_once_on_premiere_performance()
        {
            Performance performance = new Performance();
            performance.id = 1L;
            performance.play = "The CICD by Corneille";
            performance.startTime = new DateTime(2026, 11, 28, 13, 0, 0);
            performance.performanceNature = "PREMIERE";

            String reservation = theaterService.reservation(1L, 4, "STANDARD",
                performance);
            Approvals.Verify(reservation);
        }

        [TestMethod]
        public void reserve_once_on_premiere_performance_with_premium_category()
        {
            Performance performance = new Performance();
            performance.id = 1L;
            performance.play = "The CICD by Corneille";
            performance.startTime = new DateTime(2026, 11, 22, 13, 0, 0);
            performance.performanceNature = "PREMIERE";
            String reservation = theaterService.reservation(1L, 4, "PREMIUM",
                performance);
            Approvals.Verify(reservation);
        }

        [TestMethod]
        public void reserve_twice_on_premiere_performance()
        {
            Performance performance = new Performance();
            performance.id = 1L;
            performance.play = "The CICD by Corneille";
            performance.startTime = new DateTime(2023, 04, 22, 21, 0, 0);
            performance.performanceNature = "PREMIERE";
            String reservation1 = theaterService.reservation(1L, 4, "STANDARD",
                performance);
            String reservation2 = theaterService.reservation(1L, 5, "STANDARD",
                performance);
            Approvals.Verify(reservation2);
        }

        [TestMethod]
        public void reservation_failed_on_preview_performance()
        {
            Performance performance = new Performance();
            performance.id = 2L;
            performance.play = "Les fourberies de Scala - Molière";
            performance.startTime = new DateTime(2023, 03, 21, 21, 0, 0);
            performance.performanceNature = "PREVIEW";

            String reservation = theaterService.reservation(2L, 4, "STANDARD",
                performance);
            Approvals.Verify(reservation);
        }

        [TestMethod]
        public void reservation_failed_on_premiere_performance()
        {
            Performance performance = new Performance();
            performance.id = 3L;
            performance.play = "DOM JSON - Molière";
            performance.startTime = new DateTime(2023, 03, 21, 21, 0, 0);
            performance.performanceNature = "PREMIERE";

            String reservation = theaterService.reservation(2L, 4, "STANDARD",
                performance);
            Approvals.Verify(reservation);
        }

        [TestMethod]
        public void cancel_then_reserve_on_premiere_performance_with_standard_category()
        {
            Performance performance = new Performance();
            performance.id = 1L;
            performance.play = "The CICD by Corneille";
            performance.startTime = new DateTime(2023, 04, 22, 21, 0, 0);
            performance.performanceNature = "PREMIERE";
            String reservation1 = theaterService.reservation(1L, 1, "STANDARD",
                performance);
            List<string> seats = new List<string> { "B2" };
            theaterService.cancelReservation("123456", 1L, seats);
            String reservation = theaterService.reservation(1L, 4, "STANDARD",
                performance);
            Approvals.Verify(reservation);
        }        


    }
}