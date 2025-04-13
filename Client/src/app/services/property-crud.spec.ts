import { TestBed } from '@angular/core/testing';

import { CreatePropertyService } from './property-crud.service';

describe('CreatePropertyService', () => {
  let service: CreatePropertyService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CreatePropertyService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
