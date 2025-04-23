import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Chart, registerables } from 'chart.js';
import { HostService, BookingDetails } from '../../../services/host-service.service';
Chart.register(...registerables);
@Component({
    selector: 'app-earnings-chart',
    standalone: true,
    imports: [CommonModule],
    template: `
    <div class="chart-container">
 <canvas id="earningsChart"></canvas>
 </div>
 `,
  styles: [`
     .chart-container {
      width: 100%;
      max-width: 1000px;
      margin: 2rem auto;
      padding: 1rem;
      background-color: white;
      border-radius: 8px;
 box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  }
  `]
})
export class EarningsChartComponent implements OnInit {
    private chart: any;
    constructor(private hostService: HostService) { }
    ngOnInit(): void {
        this.loadEarningsData();
    }
    private loadEarningsData(): void {
        this.hostService.getAllBookings().subscribe({
            next: (bookings: BookingDetails[]) => {
                const monthlyEarnings = this.calculateMonthlyEarnings(bookings);
                this.createChart(monthlyEarnings);
            },
            error: (error) => {
                console.error('Error loading earnings data:', error);
            }
        });
    }
    private calculateMonthlyEarnings(bookings: BookingDetails[]): Map<string, number> {
        const monthlyEarnings = new Map<string, number>();

        // Filter confirmed bookings
        const confirmedBookings = bookings.filter(booking =>
            booking.status.toLowerCase() === 'confirmed'
        );
        // Calculate earnings for each month
        confirmedBookings.forEach(booking => {
            const startDate = new Date(booking.startDate);
            const monthKey = `${startDate.getFullYear()}-${String(startDate.getMonth() + 1).padStart(2, '0')}`;

            const currentEarnings = monthlyEarnings.get(monthKey) || 0;
            monthlyEarnings.set(monthKey, currentEarnings + booking.totalAmount);
        });
        // Sort by month
        return new Map([...monthlyEarnings.entries()].sort());
    }
    private createChart(monthlyEarnings: Map<string, number>): void {
        const months = Array.from(monthlyEarnings.keys());
        const earnings = Array.from(monthlyEarnings.values());
        const ctx = document.getElementById('earningsChart') as HTMLCanvasElement;

        if (this.chart) {
            this.chart.destroy();
        }
        this.chart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: months.map(month => {
                    const [year, monthNum] = month.split('-');
                    return `${new Date(parseInt(year), parseInt(monthNum) - 1).toLocaleString('default', { month: 'short' })} ${year}`;
                }),
                datasets: [{
                    label: 'Monthly Earnings ($)',
                    data: earnings,
                    borderColor: '#1976d2',
                    backgroundColor: 'rgba(25, 118, 210, 0.1)',
                    tension: 0.4,
                    fill: true
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    title: {
                        display: true,
                        text: 'Monthly Earnings from Confirmed Bookings',
                        font: {
                            size: 16,
                            weight: 'bold'
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: (context) => `$${context.parsed.y.toFixed(2)}`
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: (value) => `$${value}`
                        }
                    }
                }
            }
        });
    }
}