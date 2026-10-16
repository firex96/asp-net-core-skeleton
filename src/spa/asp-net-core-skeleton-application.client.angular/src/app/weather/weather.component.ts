import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';

interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}

@Component({
  selector: 'app-weather',
  templateUrl: './weather.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './weather.component.css'
})
export class WeatherComponent implements OnInit {
  private readonly http = inject(HttpClient);

  public forecasts?: WeatherForecast[];

  ngOnInit() {
    this.getForecasts();
  }

  getForecasts() {
    this.http.get<WeatherForecast[]>('/weatherforecast').subscribe({
      next: (result) => {
        this.forecasts = result;
      },
      error: (error) => {
        console.error(error);
      }
    });
  }
}
