import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { AuthService } from './core/auth/auth.service';

@Component({ selector: 'app-root', imports: [RouterOutlet, RouterLink, RouterLinkActive, MatButtonModule, MatToolbarModule], templateUrl: './app.html', styleUrl: './app.scss' })
export class App {
  protected readonly auth = inject(AuthService);
  protected readonly demoMenuOpen = signal(true);

  protected toggleDemoMenu(): void {
    this.demoMenuOpen.update(isOpen => !isOpen);
  }
}
