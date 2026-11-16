import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ApiService } from '../../core/api/api.service';

@Component({ selector:'app-borrower-registration', standalone:true, imports:[ReactiveFormsModule,RouterLink,MatButtonModule,MatCardModule,MatCheckboxModule,MatFormFieldModule,MatInputModule], templateUrl:'./borrower-registration.component.html', styleUrl:'./borrower-registration.component.scss' })
export class BorrowerRegistrationComponent {
  private readonly fb=inject(FormBuilder); private readonly api=inject(ApiService);
  readonly loading=signal(false); readonly error=signal(''); readonly success=signal('');
  readonly form=this.fb.nonNullable.group({ email:['',[Validators.required,Validators.email]], password:['',[Validators.required,Validators.minLength(4)]], givenName:['',Validators.required], familyName:['',Validators.required], consent:[false,Validators.requiredTrue] });
  submit():void { if(this.form.invalid){this.form.markAllAsTouched();return} this.loading.set(true);this.error.set('');const value=this.form.getRawValue();this.api.registerBorrower({email:value.email,password:value.password,givenName:value.givenName,familyName:value.familyName,consentStatus:'Granted',consentPolicyVersion:'demo-v1'}).subscribe({next:()=>{this.success.set('Your borrower account has been created. You can now sign in.');this.form.reset();this.loading.set(false)},error:()=>{this.error.set('We could not create your account. Check your details and try again.');this.loading.set(false)}}); }
}
