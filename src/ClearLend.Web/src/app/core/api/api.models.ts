export interface RegisterBorrowerRequest{email:string;password:string;givenName:string;familyName:string;consentStatus:'Granted';consentPolicyVersion:string}
export interface RegisterBorrowerResponse{userAccountId:string;borrowerProfileId:string;profileState:string}
export interface RegisterLenderRequest{email:string;password:string;legalBusinessName:string;tradingName?:string;businessRegistrationNumber?:string;contactGivenName:string;contactFamilyName:string;consentPolicyVersion:string}
export interface RegisterLenderResponse{userAccountId:string;lenderProfileId:string;profileState:string}
export interface LoginRequest{email:string;password:string}
export interface LoginResponse{accessToken:string;expiresIn:number;accountType:'Borrower'|'Lender'}
export interface BorrowerProfile{userAccountId:string;borrowerProfileId:string;email:string;givenName:string;familyName:string;accountStatus:string;profileState:string;consentStatus:string;registrationDate:string}
export interface LenderProfile{userAccountId:string;lenderProfileId:string;email:string;legalBusinessName:string;tradingName?:string;businessRegistrationNumber?:string;contactGivenName:string;contactFamilyName:string;accountStatus:string;profileState:string;consentStatus:string;registrationDate:string}
