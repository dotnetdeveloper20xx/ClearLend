import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { AuthService } from './core/auth/auth.service';

@Component({ selector: 'app-root', imports: [RouterOutlet, RouterLink, MatButtonModule, MatToolbarModule], templateUrl: './app.html', styleUrl: './app.scss' })
export class App { protected readonly auth = inject(AuthService); }
