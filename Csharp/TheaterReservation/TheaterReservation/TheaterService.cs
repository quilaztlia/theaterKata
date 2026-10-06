using System.Globalization;
using System.Text;
using TheaterReservation.Dao;
using TheaterReservation.Data;

namespace TheaterReservation
{

    public class TheaterService
    {
        private readonly TheaterRoomDao theaterRoomDao = new TheaterRoomDao();
        private readonly PerformancePriceDao performancePriceDao = new PerformancePriceDao();

        bool debug = false;


        public String reservation(Int64 customerId, int reservationCount, String reservationCategory, Performance performance)
        {
            Reservation reservation = new Reservation();
            StringBuilder sb = new StringBuilder();
            int bookedSeats = 0;
            List<String> foundSeats = new List<string>();
            Dictionary<String, String> seatsCategory = new Dictionary<string, string>();
            String zoneCategory;
            int remainingSeats = 0;
            int totalSeats = 0;
            bool foundAllSeats = false;

            sb.Append("<reservation>\n");
            sb.Append("\t<performance>\n");
            sb.Append("\t\t<play>").Append(performance.play).Append("</play>\n");
            sb.Append("\t\t<date>").Append(performance.startTime.ToString()).Append("</date>\n");
            sb.Append("\t</performance>\n");

            String res_id = ReservationService.initNewReservation();
            reservation.setReservationId(Convert.ToInt64(res_id));
            reservation.setPerformanceId(performance.id);
            sb.Append("\t<reservationId>").Append(res_id).Append("</reservationId>\n");

            TheaterRoom room = theaterRoomDao.fetchTheaterRoom(performance.id);

            // find "reservationCount" first contiguous seats in any row
            for (int i = 0; i < room.getZones().Length; i++)
            {
                Zone zone = room.getZones()[i];
                zoneCategory = zone.getCategory();
                for (int j = 0; j < zone.getRows().Length; j++)
                {
                    Row row = zone.getRows()[j];
                    List<String> seatsForRow = new List<string>();
                    int streakOfNotReservedSeats = 0;
                    for (int k = 0; k < row.getSeats().Length; k++)
                    {
                        totalSeats++; // devrait être dans une série de boucles différentes mais ça permet qq ns
                        Seat aSeat = row.getSeats()[k];
                        if (!aSeat.getStatus().Equals("BOOKED") && !aSeat.getStatus().Equals("BOOKING_PENDING"))
                        {
                            remainingSeats++;
                            if (!reservationCategory.Equals(zoneCategory))
                            {
                                continue;
                            }
                            if (!foundAllSeats)
                            {
                                seatsForRow.Add(aSeat.getSeatId());
                                streakOfNotReservedSeats++;
                                if (streakOfNotReservedSeats >= reservationCount)
                                {
                                    foreach (String seat in seatsForRow)
                                    {
                                        foundSeats.Add(seat);
                                        seatsCategory.Add(seat, zoneCategory);
                                    }
                                    foundAllSeats = true;
                                    remainingSeats -= streakOfNotReservedSeats;
                                }
                            }
                        }
                        else
                        {
                            seatsForRow = new List<string>();
                            streakOfNotReservedSeats = 0;
                        }
                    }
                    if (foundAllSeats)
                    {
                        for (int k = 0; k < row.getSeats().Length; k++)
                        {
                            Seat seat = row.getSeats()[k];
                            bookedSeats++;
                            if (foundSeats.Contains(seat.getSeatId()))
                            {
                                if (debug)
                                {
                                    Console.WriteLine("MIAOU!!! : Seat " + seat.getSeatId() + " will be saved as PENDING");
                                }
                            }
                        }

                        theaterRoomDao.saveSeats(performance.id, foundSeats, "BOOKING_PENDING");
                    }
                }
            }
            reservation.setSeats(foundSeats.ToArray());

            Console.WriteLine(remainingSeats);
            Console.WriteLine(totalSeats);
            if (foundAllSeats)
            {
                reservation.setStatus("PENDING");
            }
            else
            {
                reservation.setStatus("ABORTED");
            }

            ReservationService.updateReservation(reservation);

            if (performance.performanceNature.Equals("PREMIERE") && remainingSeats < totalSeats * 0.5)
            {
                // keep 50% seats for VIP
                foundSeats = new List<string>();
                Console.WriteLine("Not enough VIP seats available for Premiere");
            }
            else if (performance.performanceNature.Equals("PREVIEW") && remainingSeats < totalSeats * 0.9)
            {
                // keep 10% seats for VIP
                foundSeats = new List<string>();
                Console.WriteLine("Not enough VIP seats available for Preview");
            }


            if (foundSeats.Count != 0)
            {
                sb.Append("\t<reservationStatus>FULFILLABLE</reservationStatus>\n");
                sb.Append("\t\t<seats>\n");
                foreach (String s in foundSeats)
                {
                    sb.Append("\t\t\t<seat>\n");
                    sb.Append("\t\t\t\t<id>").Append(s).Append("</id>\n");
                    sb.Append("\t\t\t\t<category>").Append(seatsCategory[s]).Append("</category>\n");
                    sb.Append("\t\t\t</seat>\n");
                }
                sb.Append("\t\t</seats>\n");
            }
            else
            {
                sb.Append("\t<reservationStatus>ABORTED</reservationStatus>\n");
            }

            const decimal zero = 0m;
            const decimal one = 1m;
            decimal adjustedPrice = zero;

            // calculate raw price
            decimal myPrice = performancePriceDao.fetchPerformancePrice(performance.id);

            decimal initialPrice = Math.Round(zero, 2, MidpointRounding.ToEven);

            foreach (String foundSeat in foundSeats)
            {
                decimal categoryRatio = seatsCategory[foundSeat].Equals("STANDARD") ? one : Convert.ToDecimal("1.5", CultureInfo.InvariantCulture);
                initialPrice = initialPrice + (myPrice*categoryRatio);
            }

            // check and apply discounts and fidelity program
            decimal discountTime = VoucherProgramDao.fetchVoucherProgram(performance.startTime);

            // has he subscribed or not
            CustomerSubscriptionDao customerSubscriptionDao = new CustomerSubscriptionDao();
            bool isSubscribed = customerSubscriptionDao.fetchCustomerSubscription(customerId);

            decimal totalBilling = initialPrice;
            if (isSubscribed)
            {
                // apply a 25% discount when the user is subscribed
                decimal removePercent = Math.Round(Convert.ToDecimal("0.175", CultureInfo.InvariantCulture), 3, MidpointRounding.ToEven);
                totalBilling = (one -removePercent)* initialPrice;
            }
            decimal discountRatio = one - discountTime;
            String total = Math.Round(totalBilling * discountRatio,2, MidpointRounding.ToEven) + "€";

            sb.Append("\t<seatCategory>").Append(reservationCategory).Append("</seatCategory>\n");
            sb.Append("\t<totalAmountDue>").Append(total).Append("</totalAmountDue>\n");
            sb.Append("</reservation>\n");
            return sb.ToString();
        }

        public void cancelReservation(String reservationId, Int64 performanceId, List<String> seats)
        {
            TheaterRoom theaterRoom = theaterRoomDao.fetchTheaterRoom(performanceId);
            for (int i = 0; i < theaterRoom.getZones().Length; i++)
            {
                Zone zone = theaterRoom.getZones()[i];
                for (int j = 0; j < zone.getRows().Length; j++)
                {
                    Row row = zone.getRows()[j];
                    for (int k = 0; k < row.getSeats().Length; k++)
                    {
                        Seat seat = row.getSeats()[k];
                        if (seats.Contains(seat.getSeatId()))
                        {
                            seat.setStatus("FREE");
                        }
                    }
                }
            }
            theaterRoomDao.save(performanceId, theaterRoom);
            ReservationService.cancelReservation(Convert.ToInt64(reservationId));
        }


        public static void main(String[] args)
        {
            Performance performance = new Performance();
            performance.id = 1L;
            performance.play = "The CICD by Corneille";
            performance.startTime = new DateTime(2023, 04, 22, 21, 0, 0);
            performance.performanceNature = "PREMIERE";
            TheaterService theaterService = new TheaterService();
            Console.WriteLine(theaterService.reservation(1L, 4, "STANDARD",
                    performance));

            Console.WriteLine(theaterService.reservation(1L, 5, "STANDARD",
                    performance));

            Performance performance2 = new Performance();
            performance2.id = 2L;
            performance2.play = "Les fourberies de Scala - Molière";
            performance2.startTime = new DateTime(2023, 03, 21, 21, 0, 0);
            performance2.performanceNature = "PREVIEW";
            Console.WriteLine(theaterService.reservation(2L, 4, "STANDARD",
                    performance2));
        }
    }
}