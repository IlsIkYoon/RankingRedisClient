#include <iostream>
#include <vector>
#include <list>
#include <string>
#include <stdlib.h>
#include <iterator>


int main()
{
   int count;
   std::cin >> count; 
   std::vector<int> topArr(count);
   std::vector<int> resultArr(count);
   for(int i = 0; i < count; i++)
   {
        std::cin >> topArr[i];
   }

   for(int i = count - 1; i >= 0; i--)
   {
    for(int j = i - 1 ; j >= 0; j--)
    {
        if(topArr[j] >= topArr[i])
        {
            resultArr[i] = j + 1; 
            break;
        }    
    }
   }

   for(auto it : resultArr)
   {
    std::cout << it << ' ';
   }

    return 0;
}