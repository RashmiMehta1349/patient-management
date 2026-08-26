import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ConsultationFormComponent } from './consultation-form.component';
import { PatientService } from '../../../core/patients/patient.service';
import { Patient } from '../../../core/patients/patients.models';
import { VisitService } from '../../../core/visits/visit.service';
import { Visit } from '../../../core/visits/visits.models';
import { PrescriptionService } from '../../../core/prescriptions/prescription.service';

describe('ConsultationFormComponent', () => {
  const testPatient: Patient = {
    id: 1,
    fullName: 'Jane Doe',
    dateOfBirth: '1990-05-15',
    age: 36,
    gender: 'Female',
    phoneNumber: '555-123-4567',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  };

  const savedVisit: Visit = {
    id: 201,
    patientId: 1,
    patientName: 'Jane Doe',
    appointmentId: null,
    visitDate: new Date().toISOString(),
    temperatureValue: null,
    temperatureNotRecorded: true,
    bloodPressureValue: null,
    bloodPressureNotRecorded: true,
    pulseValue: null,
    pulseNotRecorded: true,
    complaints: null,
    diagnosis: null,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    medications: []
  };

  function setup(paramMap: Record<string, string> = {}, queryParamMap: Record<string, string> = {}) {
    TestBed.configureTestingModule({
      imports: [ConsultationFormComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap(paramMap),
              queryParamMap: convertToParamMap(queryParamMap)
            }
          }
        }
      ]
    });
  }

  it('vitals toggle mutual-exclusivity: entering a value unchecks Not recorded', () => {
    setup({}, { patientId: 'p1' });
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));

    const fixture = TestBed.createComponent(ConsultationFormComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.form.controls.temperatureNotRecorded.setValue(true);
    component.form.controls.temperatureValue.setValue(98.6);

    expect(component.form.controls.temperatureNotRecorded.value).toBeFalse();
  });

  it('vitals toggle mutual-exclusivity: checking Not recorded clears the value', () => {
    setup({}, { patientId: 'p1' });
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));

    const fixture = TestBed.createComponent(ConsultationFormComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.form.controls.pulseValue.setValue(72);
    component.form.controls.pulseNotRecorded.setValue(true);

    expect(component.form.controls.pulseValue.value).toBeNull();
  });

  it('submit with an unaddressed vital is blocked and shows inline validation', () => {
    setup({}, { patientId: 'p1' });
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    const visitService = TestBed.inject(VisitService);
    const createSpy = spyOn(visitService, 'create').and.returnValue(of(savedVisit));

    const fixture = TestBed.createComponent(ConsultationFormComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.submit();
    fixture.detectChanges();

    expect(createSpy).not.toHaveBeenCalled();
    expect(component.showVitalsValidation).toBeTrue();
  });

  it('submit with all vitals explicitly marked Not recorded succeeds', () => {
    setup({}, { patientId: 'p1' });
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    const visitService = TestBed.inject(VisitService);
    const createSpy = spyOn(visitService, 'create').and.returnValue(of(savedVisit));

    const fixture = TestBed.createComponent(ConsultationFormComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.form.controls.temperatureNotRecorded.setValue(true);
    component.form.controls.bloodPressureNotRecorded.setValue(true);
    component.form.controls.pulseNotRecorded.setValue(true);
    component.submit();
    fixture.detectChanges();

    expect(createSpy).toHaveBeenCalledWith(
      jasmine.objectContaining({
        patientId: 'p1',
        temperatureNotRecorded: true,
        bloodPressureNotRecorded: true,
        pulseNotRecorded: true
      })
    );
    expect(component.savedVisit).toEqual(savedVisit);
  });

  it('submit with mixed vitals succeeds', () => {
    setup({}, { patientId: 'p1' });
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    const visitService = TestBed.inject(VisitService);
    const createSpy = spyOn(visitService, 'create').and.returnValue(of(savedVisit));

    const fixture = TestBed.createComponent(ConsultationFormComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.form.controls.temperatureValue.setValue(99.1);
    component.form.controls.bloodPressureValue.setValue('120/80');
    component.form.controls.pulseNotRecorded.setValue(true);
    component.submit();

    expect(createSpy).toHaveBeenCalledWith(
      jasmine.objectContaining({
        temperatureValue: 99.1,
        temperatureNotRecorded: false,
        bloodPressureValue: '120/80',
        bloodPressureNotRecorded: false,
        pulseNotRecorded: true
      })
    );
  });

  it('submit error handling surfaces inline without losing entered data', () => {
    setup({}, { patientId: 'p1' });
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    const visitService = TestBed.inject(VisitService);
    spyOn(visitService, 'create').and.returnValue(throwError(() => ({ error: { message: 'Patient not found.' } })));

    const fixture = TestBed.createComponent(ConsultationFormComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.form.controls.diagnosis.setValue('Migraine');
    component.form.controls.temperatureNotRecorded.setValue(true);
    component.form.controls.bloodPressureNotRecorded.setValue(true);
    component.form.controls.pulseNotRecorded.setValue(true);
    component.submit();
    fixture.detectChanges();

    expect(component.errorMessage).toBe('Patient not found.');
    expect(component.submitting).toBeFalse();
    expect(component.form.controls.diagnosis.value).toBe('Migraine');
  });

  it('reads and pre-fills both patientId and appointmentId from route/query context', () => {
    setup({}, { patientId: '1', appointmentId: '301' });
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    const visitService = TestBed.inject(VisitService);
    const createSpy = spyOn(visitService, 'create').and.returnValue(of(savedVisit));

    const fixture = TestBed.createComponent(ConsultationFormComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    expect(component.appointmentId).toBe(301);

    component.form.controls.temperatureNotRecorded.setValue(true);
    component.form.controls.bloodPressureNotRecorded.setValue(true);
    component.form.controls.pulseNotRecorded.setValue(true);
    component.submit();

    expect(createSpy).toHaveBeenCalledWith(jasmine.objectContaining({ patientId: 1, appointmentId: 301 }));
  });

  it('edit-mode pre-populates the form from getById and submits via update()', () => {
    setup({ id: 'v1' });
    const visitService = TestBed.inject(VisitService);
    const existing: Visit = { ...savedVisit, temperatureValue: 98.6, temperatureNotRecorded: false, diagnosis: 'Flu' };
    spyOn(visitService, 'getById').and.returnValue(of(existing));
    const updateSpy = spyOn(visitService, 'update').and.returnValue(of(existing));

    const fixture = TestBed.createComponent(ConsultationFormComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    expect(component.isEditMode).toBeTrue();
    expect(component.form.value.temperatureValue).toBe(98.6);
    expect(component.form.value.diagnosis).toBe('Flu');

    component.form.controls.diagnosis.setValue('Updated diagnosis');
    component.submit();

    expect(updateSpy).toHaveBeenCalledWith('v1', jasmine.objectContaining({ diagnosis: 'Updated diagnosis' }));
  });

  it('adding and removing medication rows updates form state correctly', () => {
    setup({}, { patientId: 'p1' });
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));

    const fixture = TestBed.createComponent(ConsultationFormComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.addMedicationRow();
    component.addMedicationRow();
    expect(component.medications.length).toBe(2);

    component.removeMedicationRow(0);
    expect(component.medications.length).toBe(1);
  });

  it('submit includes all non-blank medication rows, dropping a fully-blank trailing row', () => {
    setup({}, { patientId: 'p1' });
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    const visitService = TestBed.inject(VisitService);
    const createSpy = spyOn(visitService, 'create').and.returnValue(of(savedVisit));

    const fixture = TestBed.createComponent(ConsultationFormComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.form.controls.temperatureNotRecorded.setValue(true);
    component.form.controls.bloodPressureNotRecorded.setValue(true);
    component.form.controls.pulseNotRecorded.setValue(true);

    component.addMedicationRow();
    component.medications.at(0).patchValue({
      name: 'Paracetamol',
      dosage: '500mg',
      frequency: 'Twice daily',
      duration: '5 days',
      instructions: 'After food'
    });
    component.addMedicationRow(); // fully-blank trailing row

    component.submit();

    expect(createSpy).toHaveBeenCalledWith(
      jasmine.objectContaining({
        medications: [
          { name: 'Paracetamol', dosage: '500mg', frequency: 'Twice daily', duration: '5 days', instructions: 'After food' }
        ]
      })
    );
  });

  it('submit with zero medication rows succeeds (no forced minimum)', () => {
    setup({}, { patientId: 'p1' });
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    const visitService = TestBed.inject(VisitService);
    const createSpy = spyOn(visitService, 'create').and.returnValue(of(savedVisit));

    const fixture = TestBed.createComponent(ConsultationFormComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.form.controls.temperatureNotRecorded.setValue(true);
    component.form.controls.bloodPressureNotRecorded.setValue(true);
    component.form.controls.pulseNotRecorded.setValue(true);
    component.submit();

    expect(createSpy).toHaveBeenCalledWith(jasmine.objectContaining({ medications: [] }));
  });

  it('submit with a touched-but-incomplete medication row is blocked and shows inline validation', () => {
    setup({}, { patientId: 'p1' });
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    const visitService = TestBed.inject(VisitService);
    const createSpy = spyOn(visitService, 'create').and.returnValue(of(savedVisit));

    const fixture = TestBed.createComponent(ConsultationFormComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.form.controls.temperatureNotRecorded.setValue(true);
    component.form.controls.bloodPressureNotRecorded.setValue(true);
    component.form.controls.pulseNotRecorded.setValue(true);

    component.addMedicationRow();
    component.medications.at(0).patchValue({ name: 'Ibuprofen' }); // only Name filled

    component.submit();

    expect(createSpy).not.toHaveBeenCalled();
    expect(component.showMedicationValidation).toBeTrue();
  });

  it('printPrescription fetches the PDF and opens it', () => {
    setup({}, { patientId: 'p1' });
    const patientService = TestBed.inject(PatientService);
    spyOn(patientService, 'getById').and.returnValue(of(testPatient));
    const prescriptionService = TestBed.inject(PrescriptionService);
    const blob = new Blob(['%PDF-1.4'], { type: 'application/pdf' });
    const pdfSpy = spyOn(prescriptionService, 'getPrescriptionPdf').and.returnValue(of(blob));
    spyOn(window, 'open').and.returnValue({} as Window);
    spyOn(window.URL, 'createObjectURL').and.returnValue('blob:mock');
    spyOn(window.URL, 'revokeObjectURL');

    const fixture = TestBed.createComponent(ConsultationFormComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.printPrescription(201);

    expect(pdfSpy).toHaveBeenCalledWith(201);
    expect(component.printing).toBeFalse();
    expect(component.printError).toBeFalse();
  });
});
