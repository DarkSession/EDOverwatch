import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'dateAgo'
})
export class DateAgoPipe implements PipeTransform {

  public transform(value: any, args?: any): any {
    if (value) {
      let seconds = Math.floor((+new Date() - +new Date(value)) / 1000);
      const future = (seconds < 0);
      seconds = Math.abs(seconds);
      if (seconds < 29) {
        return 'Just now';
      }
      let units = 2;
      if (args) {
        const arg = parseInt(args);
        if (!isNaN(arg)) {
          units = arg;
        }
      }
      const intervals: { [key: string]: number } = {
        'year': 31536000,
        'month': 2592000,
        'week': 604800,
        'day': 86400,
        'hour': 3600,
        'minute': 60,
        'second': 1,
      };
      let unitResults = [];
      for (const i in intervals) {
        let counter = Math.floor(seconds / intervals[i]);
        if (counter > 0) {
          const isLastUnit = (unitResults.length + 1 === units);
          if (isLastUnit) {
            counter = Math.round(seconds / intervals[i]);
          }
          seconds -= (counter * intervals[i]);
          if (counter === 1) {
            unitResults.push(counter + ' ' + i);
          } else {
            unitResults.push(counter + ' ' + i + 's');
          }
          if (isLastUnit) {
            break;
          }
        }
      }
      if (unitResults.length === 0) {
        return "";
      }
      const unitResult = unitResults.slice(0, units).join(", ");
      if (future) {
        return unitResult;
      }
      return unitResult + " ago";
    }
    return value;
  }
}
