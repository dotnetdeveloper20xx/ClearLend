import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
@Component({ selector: 'app-how-it-works', standalone: true, imports: [RouterLink, MatCardModule], templateUrl: './how-it-works.component.html', styleUrl: './how-it-works.component.scss' })
export class HowItWorksComponent { }
