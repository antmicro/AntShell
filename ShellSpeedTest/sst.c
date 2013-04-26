#include <termios.h>
#include <unistd.h>
#include <sys/time.h>
#include <stdio.h>

/* Return 1 if the difference is negative, otherwise 0.  */
int timeval_subtract(struct timeval *result, struct timeval *t2, struct timeval *t1)
{
    long int diff = (t2->tv_usec + 1000000 * t2->tv_sec) - (t1->tv_usec + 1000000 * t1->tv_sec);
    result->tv_sec = diff / 1000000;
    result->tv_usec = diff % 1000000;

    return (diff<0);
}

int main()
{
  struct termios t, o;
  struct timeval begin, end, diff;

  tcgetattr(0, &t);
  tcgetattr(0, &o);
  cfmakeraw(&t);
  tcsetattr(0, 1, &t);

  char r;
  char gpc[] = { 27, '[', '6', 'n' };

  gettimeofday(&begin, NULL);

  int i;
  for (i=0; i < 100; i++)
  {
    write(0, gpc, 4);

    read(1, &r, 1); // ESC
    read(1, &r, 1); // [
    do {
      read(1, &r, 1); 
    } while (r != ';');

    do {
      read(1, &r, 1);
    } while (r != 'R');
  }

  gettimeofday(&end, NULL);

  timeval_subtract(&diff, &end, &begin);
  printf("Time elapsed: %dms\n", (diff.tv_usec + 1000000 * diff.tv_sec) / 1000);

  tcsetattr(0, 1, &o);

  return 0;
}
