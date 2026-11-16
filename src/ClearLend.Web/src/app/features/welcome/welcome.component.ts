import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-welcome',
  standalone: true,
  imports: [RouterLink],
  styleUrl: './welcome.component.scss',
  template: `<section class="welcome">
    <div class="hero-copy">
      <p class="eyebrow">A clearer way forward</p>
      <h1>Finance built around <em>trust.</em></h1>
      <p class="intro">ClearLend brings borrowers and business lenders together with clarity at every step. Choose how you want to get started.</p>
      <div class="hero-actions"><a class="primary" routerLink="/lender/register">Join as a lender <span>→</span></a><a class="secondary" routerLink="/register">I need to borrow <span>→</span></a></div>
    </div>
    <div class="pathways">
      <div class="pathway borrower"><div class="icon">↗</div><p class="label">For borrowers</p><h2>Find your next opportunity.</h2><p>Build your profile and take the first step toward funding that makes sense for you.</p><a routerLink="/register">Create a borrower account →</a></div>
      <div class="pathway lender"><div class="icon">◇</div><p class="label">For business lenders</p><h2>Put your capital to work.</h2><p>Set up your business profile and prepare to connect with the right opportunities.</p><a routerLink="/lender/register">Create a lender account →</a></div>
    </div>
    <p class="signin">Already have an account? <a routerLink="/login">Sign in securely</a></p>
  </section>`
})
export class WelcomeComponent { }
