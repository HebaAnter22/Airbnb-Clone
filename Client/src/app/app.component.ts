import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import {  WeatherComponent} from './components/weather-list/weather';
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, WeatherComponent],
  template: `
  <div class="container">
    <app-weather></app-weather>
  </div>`
})
export class AppComponent {
  title = 'Airbnb';
}
