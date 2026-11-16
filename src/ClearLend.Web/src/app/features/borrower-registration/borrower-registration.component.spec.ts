import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { BorrowerRegistrationComponent } from './borrower-registration.component';

describe('BorrowerRegistrationComponent', () => {
  const api = { registerBorrower: vi.fn(() => of({ userAccountId: 'user-1', borrowerProfileId: 'borrower-1', profileState: 'Completed' })) };

  beforeEach(async () => {
    vi.clearAllMocks();
    await TestBed.configureTestingModule({ imports: [BorrowerRegistrationComponent], providers: [provideRouter([]), { provide: ApiService, useValue: api }] }).compileComponents();
  });

  it('does not submit an invalid form', () => {
    const fixture = TestBed.createComponent(BorrowerRegistrationComponent);
    fixture.componentInstance.submit();
    expect(api.registerBorrower).not.toHaveBeenCalled();
  });

  it('submits valid borrower details and shows success', () => {
    const fixture = TestBed.createComponent(BorrowerRegistrationComponent);
    const component = fixture.componentInstance;
    component.form.setValue({ email: 'faz@example.com', password: 'a-secure-password', givenName: 'Faz', familyName: 'Ahmed', consent: true });
    component.submit();
    expect(api.registerBorrower).toHaveBeenCalledOnce();
    expect(component.success()).toContain('created');
  });
});
