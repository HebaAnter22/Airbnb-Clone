import { Component, OnInit } from '@angular/core';
import { WeatherService } from '../../services/weather-api.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-weather',
  standalone: true,
  imports: [CommonModule],
  template: `
    <h2>Weather Forecast</h2>
    <div *ngIf="loading">Loading...</div>
    <div *ngIf="error" class="error">{{ error }}</div>
    
    <table *ngIf="forecasts">
      <thead>
        <tr>
          <th>Date</th>
          <th>Temp (°C)</th>
          <th>Temp (°F)</th>
          <th>Summary</th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let forecast of forecasts">
          <td>{{ forecast.date  }}</td>
          <td>{{ forecast.temperatureC }}</td>
          <td>{{ forecast.temperatureF }}</td>
          <td>{{ forecast.summary }}</td>
        </tr>
      </tbody>
    </table>
  `,
  styles: [`
    table {
      width: 100%;
      border-collapse: collapse;
      margin-top: 20px;
    }
    th, td {
      padding: 8px;
      text-align: left;
      border-bottom: 1px solid #ddd;
    }
    .error {
      color: red;
      margin: 20px 0;
    }
  `]
})
export class WeatherComponent implements OnInit {
  forecasts: any[] | null = null;
  loading = true;
  error: string | null = null;

  constructor(private weatherService: WeatherService) {}

  ngOnInit(): void {
    this.weatherService.getWeatherForecast().subscribe({
      next: (data) => {
        this.forecasts = data;
        this.loading = false;
      },
error: (err) => {
  console.error('API Error:', err);
  this.error = `Failed to load weather data: ${err.message}`;
  this.loading = false;
}
    });
  }
}