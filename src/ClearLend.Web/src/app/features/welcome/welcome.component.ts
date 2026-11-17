import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-welcome',
  standalone: true,
  imports: [RouterLink],
  styleUrl: './welcome.component.scss',
  template: `<section class="welcome">
    <div class="announcement"><span class="announcement-dot" aria-hidden="true"></span><span>ClearLend demo environment · Explore the borrower, lender, and operations journeys</span></div>
    <div class="hero-panel">
    <div class="hero-copy">
      <p class="eyebrow">A clearer way forward</p>
      <h1>Finance built around <em>trust.</em></h1>
      <p class="intro">ClearLend brings borrowers, business lenders, and operations teams together with clarity at every step.</p>
      <div class="hero-actions"><a class="primary" routerLink="/register">Start as a borrower <span>→</span></a><a class="secondary" routerLink="/lender/register">Join as a lender <span>→</span></a></div>
      <p class="signin">Already have an account? <a routerLink="/login">Sign in securely</a></p>
    </div>
    <div class="hero-aside" aria-label="ClearLend platform summary"><p class="aside-label">One connected journey</p><div class="journey-line"><span>01</span><strong>Register</strong><small>Choose your path</small></div><div class="journey-line"><span>02</span><strong>Get ready</strong><small>Build confidence</small></div><div class="journey-line"><span>03</span><strong>Move forward</strong><small>Make informed decisions</small></div></div>
    </div>
    <div class="section-intro"><p class="eyebrow">Built for every side of finance</p><h2>A platform with a clear point of view.</h2></div>
    <div class="pathways">
      <div class="pathway borrower"><div class="icon">↗</div><p class="label">For borrowers</p><h3>Find your next opportunity.</h3><p>Build your profile and take the first step toward funding that makes sense for you.</p><a routerLink="/register">Create a borrower account →</a></div>
      <div class="pathway lender"><div class="icon">◇</div><p class="label">For business lenders</p><h3>Put your capital to work.</h3><p>Set up your business profile and prepare to connect with the right opportunities.</p><a routerLink="/lender/register">Create a lender account →</a></div>
      <div class="pathway operations"><div class="icon">◎</div><p class="label">For operations teams</p><h3>Keep every decision clear.</h3><p>Support the journey with organised review, verification, and staff controls.</p><a routerLink="/login">Sign in to the staff area →</a></div>
    </div>
  </section>`
})
export class WelcomeComponent { }
